﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;
using Microsoft.Practices.Prism.Commands;
using Samba.Domain.Models.Menus;
using Samba.Domain.Models.Tickets;
using Samba.Infrastructure;
using Samba.Localization.Properties;
using Samba.Persistance.Data;
using Samba.Presentation.Common;
using Samba.Presentation.Common.Services;
using Samba.Presentation.ViewModels;
using Samba.Services;

namespace Samba.Modules.TicketModule
{
    public class SelectedTicketItemsViewModel : ObservableObject
    {
        private bool _showExtraPropertyEditor;
        private bool _showTicketNoteEditor;
        private bool _showFreeTagEditor;
        private bool _removeModifier;

        public SelectedTicketItemsViewModel()
        {
            
            CloseCommand = new CaptionCommand<string>(Resources.Close, OnCloseCommandExecuted);
            SelectReasonCommand = new DelegateCommand<int?>(OnReasonSelected);
            SelectTicketTagCommand = new DelegateCommand<TicketTag>(OnTicketTagSelected);
            PortionSelectedCommand = new DelegateCommand<MenuItemPortion>(OnPortionSelected);
            PropertySelectedCommand = new DelegateCommand<MenuItemProperty>(OnPropertySelected);
            PropertyGroupSelectedCommand = new DelegateCommand<MenuItemGroupedPropertyItemViewModel>(OnPropertyGroupSelected);
            RemoveModifierCommand = new CaptionCommand<string>(Resources.RemoveModifier, OnRemoveModifier);
            UpdateExtraPropertiesCommand = new CaptionCommand<string>(Resources.Update, OnUpdateExtraProperties);
            UpdateFreeTagCommand = new CaptionCommand<string>(Resources.AddAndSave, OnUpdateFreeTag, CanUpdateFreeTag);
            SelectedItemPortions = new ObservableCollection<MenuItemPortion>();
            SelectedItemPropertyGroups = new ObservableCollection<MenuItemPropertyGroup>();
            SelectedItemGroupedPropertyItems = new ObservableCollection<MenuItemGroupedPropertyViewModel>();
            Reasons = new ObservableCollection<Reason>();
            TicketTags = new ObservableCollection<TicketTag>();
            EventServiceFactory.EventService.GetEvent<GenericEvent<TicketViewModel>>().Subscribe(OnTicketViewModelEvent);
        }

        private void OnTicketViewModelEvent(EventParameters<TicketViewModel> obj)
        {
            if (obj.Topic == EventTopicNames.SelectTicketTag)
            {
                ResetValues(obj.Value);
                _showFreeTagEditor = SelectedTicket.LastSelectedTicketTag.FreeTagging;

                List<TicketTag> tags;
                if (_showFreeTagEditor)
                {
                    tags = Dao.Query<TicketTagGroup>(x => x.Id == SelectedTicket.LastSelectedTicketTag.Id,
                                                 x => x.TicketTags).SelectMany(x => x.TicketTags).OrderBy(x => x.Name).ToList();
                }
                else
                {
                    tags = AppServices.MainDataContext.SelectedDepartment.TicketTagGroups.Where(
                           x => x.Name == obj.Value.LastSelectedTicketTag.Name).SelectMany(x => x.TicketTags).ToList();
                }
                tags.Sort(new AlphanumComparator());
                TicketTags.AddRange(tags);

                if (SelectedTicket.IsTaggedWith(SelectedTicket.LastSelectedTicketTag.Name)) TicketTags.Add(TicketTag.Empty);
                if (TicketTags.Count == 1 && !_showFreeTagEditor) obj.Value.UpdateTag(SelectedTicket.LastSelectedTicketTag, TicketTags[0]);
                RaisePropertyChanged("TagColumnCount");
                RaisePropertyChanged("IsFreeTagEditorVisible");
                RaisePropertyChanged("FilteredTextBoxType");
            }

            if (obj.Topic == EventTopicNames.SelectVoidReason)
            {
                ResetValues(obj.Value);
                Reasons.AddRange(AppServices.MainDataContext.Reasons.Values.Where(x => x.ReasonType == 0));
                if (Reasons.Count == 0) obj.Value.VoidSelectedItems(0);
                RaisePropertyChanged("ReasonColumnCount");
            }

            if (obj.Topic == EventTopicNames.SelectGiftReason)
            {
                ResetValues(obj.Value);
                Reasons.AddRange(AppServices.MainDataContext.Reasons.Values.Where(x => x.ReasonType == 1));
                if (Reasons.Count == 0) obj.Value.GiftSelectedItems(0);
                RaisePropertyChanged("ReasonColumnCount");
            }

            if (obj.Topic == EventTopicNames.AddExtraModifiers)
            {
                //ResetValues(obj.Value);
                //if (SelectedTicket != null && !SelectedItem.Model.Voided && !SelectedItem.Model.Locked)
                //{
                //    var id = SelectedItem.Model.MenuItemId;
                //    var mi = AppServices.DataAccessService.GetMenuItem(id);
                 
                //    SelectedItemPropertyGroups.AddRange(mi.PropertyGroups.Where(x => string.IsNullOrEmpty(x.GroupTag)));
               //     SelectedItemPortions.Clear();
                //    
                 //   SelectedItemGroupedPropertyItems.Clear();
               // }
              //  RaisePropertyChanged("ColumnCount");
               
            }

            if (obj.Topic == EventTopicNames.SelectExtraProperty)
            {
                ResetValues(obj.Value);
                _showExtraPropertyEditor = true;
                RaisePropertyChanged("IsExtraPropertyEditorVisible");
                RaisePropertyChanged("IsPortionsVisible");
            }

            if (obj.Topic == EventTopicNames.EditTicketNote)
            {
                ResetValues(obj.Value);
                _showTicketNoteEditor = true;
                RaisePropertyChanged("IsTicketNoteEditorVisible");
            }
        }

        private void ResetValues(TicketViewModel selectedTicket)
        {
            SelectedTicket = null;
            SelectedItem = null;
            SelectedItemPortions.Clear();
            SelectedItemPropertyGroups.Clear();
            SelectedItemGroupedPropertyItems.Clear();
            Reasons.Clear();
            TicketTags.Clear();
            _showExtraPropertyEditor = false;
            _showTicketNoteEditor = false;
            _showFreeTagEditor = false;
            _removeModifier = false;
            SetSelectedTicket(selectedTicket);
        }

        public TicketViewModel SelectedTicket { get; private set; }
        public TicketItemViewModel SelectedItem { get; private set; }

        public ICaptionCommand CloseCommand { get; set; }
        public ICaptionCommand RemoveModifierCommand { get; set; }
        public ICaptionCommand UpdateExtraPropertiesCommand { get; set; }
        public ICaptionCommand UpdateFreeTagCommand { get; set; }
        public ICommand SelectReasonCommand { get; set; }
        public ICommand SelectTicketTagCommand { get; set; }

        public DelegateCommand<MenuItemPortion> PortionSelectedCommand { get; set; }
        public ObservableCollection<MenuItemPortion> SelectedItemPortions { get; set; }

        public DelegateCommand<MenuItemProperty> PropertySelectedCommand { get; set; }
        public ObservableCollection<MenuItemPropertyGroup> SelectedItemPropertyGroups { get; set; }
        public ObservableCollection<MenuItemGroupedPropertyViewModel> SelectedItemGroupedPropertyItems { get; set; }

        public DelegateCommand<MenuItemGroupedPropertyItemViewModel> PropertyGroupSelectedCommand { get; set; }

        public ObservableCollection<Reason> Reasons { get; set; }
        public ObservableCollection<TicketTag> TicketTags { get; set; }

        public int ReasonColumnCount { get { return Reasons.Count % 7 == 0 ? Reasons.Count / 7 : (Reasons.Count / 7) + 1; } }
        public int TagColumnCount { get { return TicketTags.Count % 7 == 0 ? TicketTags.Count / 7 : (TicketTags.Count / 7) + 1; } }

        public FilteredTextBox.FilteredTextBoxType FilteredTextBoxType
        {
            get
            {
                if (SelectedTicket != null && SelectedTicket.LastSelectedTicketTag != null && SelectedTicket.LastSelectedTicketTag.NumericTags)
                    return FilteredTextBox.FilteredTextBoxType.Digits;
                if (SelectedTicket != null && SelectedTicket.LastSelectedTicketTag != null && SelectedTicket.LastSelectedTicketTag.PriceTags)
                    return FilteredTextBox.FilteredTextBoxType.Number;
                return FilteredTextBox.FilteredTextBoxType.Letters;
            }
        }

        private string _freeTag;
        public string FreeTag
        {
            get { return _freeTag; }
            set
            {
                _freeTag = value;
                RaisePropertyChanged("FreeTag");
            }
        }

        public bool IsPortionsVisible
        {
            get
            {
                return SelectedItem != null
                    && Reasons.Count == 0
                    && !SelectedItem.IsVoided
                    && !SelectedItem.IsLocked
                    && SelectedItemPortions.Count > 0;
            }
        }

        public string RemoveModifierButtonColor { get { return _removeModifier ? "Red" : "Black"; } }
        public bool IsRemoveModifierButtonVisible { get { return SelectedItem != null && SelectedItem.Properties.Count > 0; } }

        private void OnRemoveModifier(string obj)
        {
            _removeModifier = !_removeModifier;
            RaisePropertyChanged("RemoveModifierButtonColor");
        }

        private void OnCloseCommandExecuted(string obj)
        {
            var unselectedItem = SelectedItemPropertyGroups.FirstOrDefault(x => x.ForceValue && SelectedItem.Properties.Count(y => y.Model.PropertyGroupId == x.Id) == 0);
            if (unselectedItem != null)
            {
                InteractionService.UserIntraction.GiveFeedback(string.Format(Resources.SelectionRequired_f, unselectedItem.Name));
                return;
            }

            _showTicketNoteEditor = false;
            _showExtraPropertyEditor = false;
            _showFreeTagEditor = false;
            _removeModifier = false;
            FreeTag = string.Empty;
            SelectedTicket.ClearSelectedItems();
        }

        public bool IsFreeTagEditorVisible { get { return _showFreeTagEditor; } }
        public bool IsExtraPropertyEditorVisible { get { return _showExtraPropertyEditor && SelectedItem != null; } }
        public bool IsTicketNoteEditorVisible { get { return _showTicketNoteEditor; } }


        private bool CanUpdateFreeTag(string arg)
        {
            return !string.IsNullOrEmpty(FreeTag);
        }

        private void OnUpdateFreeTag(string obj)
        {
            var cachedTag = AppServices.MainDataContext.SelectedDepartment.TicketTagGroups.Single(
                x => x.Id == SelectedTicket.LastSelectedTicketTag.Id);
            Debug.Assert(cachedTag != null);
            var ctag = cachedTag.TicketTags.SingleOrDefault(x => x.Name.ToLower() == FreeTag.ToLower());
            if (ctag == null && cachedTag.SaveFreeTags)
            {
                using (var workspace = WorkspaceFactory.Create())
                {
                    var tt = workspace.Single<TicketTagGroup>(x => x.Id == SelectedTicket.LastSelectedTicketTag.Id);
                    Debug.Assert(tt != null);
                    var tag = tt.TicketTags.SingleOrDefault(x => x.Name.ToLower() == FreeTag.ToLower());
                    if (tag == null)
                    {
                        tag = new TicketTag { Name = FreeTag };
                        tt.TicketTags.Add(tag);
                        workspace.Add(tag);
                        workspace.CommitChanges();
                    }
                }
            }
            SelectedTicket.UpdateTag(SelectedTicket.LastSelectedTicketTag, new TicketTag { Name = FreeTag });
            FreeTag = string.Empty;
        }

        private void OnUpdateExtraProperties(string obj)
        {
            SelectedTicket.RefreshVisuals();
            _showExtraPropertyEditor = false;
            RaisePropertyChanged("IsExtraPropertyEditorVisible");
        }

        private void OnTicketTagSelected(TicketTag obj)
        {
            SelectedTicket.UpdateTag(SelectedTicket.LastSelectedTicketTag, obj);
        }

        private void OnReasonSelected(int? reasonId)
        {
            var rid = reasonId.GetValueOrDefault(0);
            Reason r = AppServices.MainDataContext.Reasons[rid];
            if (r.ReasonType == 0)
                SelectedTicket.VoidSelectedItems(rid);
            if (r.ReasonType == 1)
                SelectedTicket.GiftSelectedItems(rid);
        }

        public void OnPortionSelected(MenuItemPortion obj)
        {
            SelectedItem.UpdatePortion(obj, AppServices.MainDataContext.SelectedDepartment.PriceTag);
            SelectedTicket.RefreshVisuals();
            SelectedTicket.RecalculateTicket();
            if (SelectedItemPropertyGroups.Count == 0 && SelectedItemGroupedPropertyItems.Count == 0)
                SelectedTicket.ClearSelectedItems();
        }

        private void OnPropertySelected(MenuItemProperty obj)
        {
            var mig = SelectedItemPropertyGroups.FirstOrDefault(propertyGroup => propertyGroup.Properties.Contains(obj));
            Debug.Assert(mig != null);
            if (_removeModifier)
            {
                if (mig.ForceValue && SelectedItem.Properties.Count(x => x.Model.PropertyGroupId == mig.Id) < 2)
                    InteractionService.UserIntraction.GiveFeedback(string.Format(Resources.SelectionRequired_f, mig.Name));
                else
                    if (!SelectedItem.RemoveProperty(mig, obj))
                    {
                        InteractionService.UserIntraction.GiveFeedback(string.Format(Resources.SelectionRequired_f, mig.Name + ":" + obj.Name));
                    }
            }
            else SelectedItem.ToggleProperty(mig, obj);
            SelectedTicket.RefreshVisuals();
            SelectedTicket.RecalculateTicket();
            if (_removeModifier)
                OnRemoveModifier("");
            RaisePropertyChanged("IsRemoveModifierButtonVisible");
        }

        private void OnPropertyGroupSelected(MenuItemGroupedPropertyItemViewModel obj)
        {
            if (_removeModifier)
            {
                SelectedItem.RemoveProperty(obj.MenuItemPropertyGroup, obj.CurrentProperty);
                obj.UpdateNextProperty(null);
            }
            else
            {
                SelectedItem.ToggleProperty(obj.MenuItemPropertyGroup, obj.NextProperty);
                obj.UpdateNextProperty(obj.NextProperty);
            }
            SelectedTicket.RefreshVisuals();
            SelectedTicket.RecalculateTicket();
            if (_removeModifier)
                OnRemoveModifier("");
            RaisePropertyChanged("IsRemoveModifierButtonVisible");
        }

        private void SetSelectedTicket(TicketViewModel ticketViewModel)
        {
            SelectedTicket = ticketViewModel;
            SelectedItem = SelectedTicket.SelectedItems.Count() == 1 ? SelectedTicket.SelectedItems[0] : null;
            RaisePropertyChanged("SelectedTicket");
            RaisePropertyChanged("SelectedItem");
            RaisePropertyChanged("IsTicketNoteEditorVisible");
            RaisePropertyChanged("IsExtraPropertyEditorVisible");
            RaisePropertyChanged("IsFreeTagEditorVisible");
            RaisePropertyChanged("IsPortionsVisible");
            RaisePropertyChanged("IsRemoveModifierButtonVisible");
        }

        public bool ShouldDisplay(TicketViewModel value)
        {
            ResetValues(value);

            if (SelectedItem == null || SelectedItem.Model.Locked) return false;

            if (SelectedTicket != null && !SelectedItem.Model.Voided && !SelectedItem.Model.Locked)
            {
                var id = SelectedItem.Model.MenuItemId;

                var mi = AppServices.DataAccessService.GetMenuItem(id);
                if (SelectedItem.Model.PortionCount > 1) SelectedItemPortions.AddRange(mi.Portions);
                SelectedItemPropertyGroups.AddRange(mi.PropertyGroups.Where(x => string.IsNullOrEmpty(x.GroupTag)));

                //rjoshi sort displayed item properties
                //for (int i = 1; i <  SelectedItemPropertyGroups.Count; i++)
                //{
                //    IEnumerable<MenuItemProperty> sortedEnum = SelectedItemPropertyGroups[i].Properties.OrderBy(x => x.Name); ;
                //    SelectedItemPropertyGroups[i].Properties = sortedEnum.ToList();
                     
                //}

                SelectedItemGroupedPropertyItems.AddRange(mi.PropertyGroups.Where(x => !string.IsNullOrEmpty(x.GroupTag) && x.Properties.Count > 1)
                    .GroupBy(x => x.GroupTag)
                    .Select(x => new MenuItemGroupedPropertyViewModel(SelectedItem, x)).OrderBy(x => x.Name));
                RaisePropertyChanged("IsPortionsVisible");
            }
            return SelectedItemPortions.Count > 1 || SelectedItemPropertyGroups.Count > 0 || SelectedItemGroupedPropertyItems.Count > 0;
        }
    }
}
