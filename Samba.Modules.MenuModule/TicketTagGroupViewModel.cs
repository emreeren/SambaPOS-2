using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Samba.Domain.Models.Settings;
using Samba.Domain.Models.Tickets;
using Samba.Infrastructure.Data;
using Samba.Localization.Properties;
using Samba.Presentation.Common;
using Samba.Presentation.Common.ModelBase;
using Samba.Presentation.ViewModels;

namespace Samba.Modules.MenuModule
{
    public class TicketTagGroupViewModel : EntityViewModelBase<TicketTagGroup>
    {
        private IWorkspace _workspace;

        private readonly IList<string> _actions = new[] { Resources.Refresh, Resources.CloseTicket, Resources.GetPayment };
        public IList<string> Actions { get { return _actions; } }

        private readonly IList<string> _tagTypes = new[] { Resources.AlphaNumeric, Resources.Numeric, Resources.Price };
        public IList<string> TagTypes { get { return _tagTypes; } }

        private IEnumerable<Numerator> _numerators;
        public IEnumerable<Numerator> Numerators
        {
            get { return _numerators ?? (_numerators = _workspace.All<Numerator>()); }
        }

        private readonly ObservableCollection<TicketTagViewModel> _ticketTags;
        public ObservableCollection<TicketTagViewModel> TicketTags { get { return _ticketTags; } }

        public TicketTagViewModel SelectedTicketTag { get; set; }
        public ICaptionCommand AddTicketTagCommand { get; set; }
        public ICaptionCommand DeleteTicketTagCommand { get; set; }

        public string Action { get { return Actions[Model.Action]; } set { Model.Action = Actions.IndexOf(value); } }
        public Numerator Numerator { get { return Model.Numerator; } set { Model.Numerator = value; } }
        public bool FreeTagging { get { return Model.FreeTagging; } set { Model.FreeTagging = value; } }
        public bool SaveFreeTags { get { return Model.SaveFreeTags; } set { Model.SaveFreeTags = value; } }
        public bool ForceValue { get { return Model.ForceValue; } set { Model.ForceValue = value; } }
        public bool NumericTags { get { return Model.NumericTags; } set { Model.NumericTags = value; } }
        public bool PriceTags { get { return Model.PriceTags; } set { Model.PriceTags = value; } }
        public string ButtonColorWhenTagSelected { get { return Model.ButtonColorWhenTagSelected; } set { Model.ButtonColorWhenTagSelected = value; } }
        public string ButtonColorWhenNoTagSelected { get { return Model.ButtonColorWhenNoTagSelected; } set { Model.ButtonColorWhenNoTagSelected = value; } }
        public bool ActiveOnPosClient { get { return Model.ActiveOnPosClient; } set { Model.ActiveOnPosClient = value; } }
        public bool ActiveOnTerminalClient { get { return Model.ActiveOnTerminalClient; } set { Model.ActiveOnTerminalClient = value; } }
        public bool ExcludeInReports { get { return Model.ExcludeInReports; } set { Model.ExcludeInReports = value; } }

        public string TaggingType { get { return TagTypes[SelectedTaggingType]; } set { SelectedTaggingType = TagTypes.IndexOf(value); } }

        public int SelectedTaggingType
        {
            get
            {
                if (NumericTags && !PriceTags) return 1;
                if (!NumericTags && PriceTags) return 2;
                return 0;
            }
            set
            {
                if (value == 1)
                {
                    NumericTags = true;
                    PriceTags = false;
                }
                else if (value == 2)
                {
                    NumericTags = false;
                    PriceTags = true;
                }
                else
                {
                    NumericTags = false;
                    PriceTags = false;
                }
            }
        }

        public TicketTagGroupViewModel(TicketTagGroup model)
            : base(model)
        {
            _ticketTags = new ObservableCollection<TicketTagViewModel>(GetTicketTags(model));
            AddTicketTagCommand = new CaptionCommand<string>(string.Format(Resources.Add_f, Resources.Tag), OnAddTicketTagExecuted);
            DeleteTicketTagCommand = new CaptionCommand<string>(string.Format(Resources.Delete_f, Resources.Tag), OnDeleteTicketTagExecuted, CanDeleteTicketTag);
        }

        private static IEnumerable<TicketTagViewModel> GetTicketTags(TicketTagGroup ticketTagGroup)
        {
            return ticketTagGroup.TicketTags.Select(item => new TicketTagViewModel(item));
        }

        public override string GetModelTypeString()
        {
            return Resources.TicketTag;
        }

        protected override void Initialize(IWorkspace workspace)
        {
            _workspace = workspace;
        }

        public override Type GetViewType()
        {
            return typeof(TicketTagGroupView);
        }

        private bool CanDeleteTicketTag(string arg)
        {
            return SelectedTicketTag != null;
        }

        private void OnDeleteTicketTagExecuted(string obj)
        {
            if (SelectedTicketTag == null) return;
            if (SelectedTicketTag.Model.Id > 0)
                _workspace.Delete(SelectedTicketTag.Model);
            Model.TicketTags.Remove(SelectedTicketTag.Model);
            TicketTags.Remove(SelectedTicketTag);
        }

        private void OnAddTicketTagExecuted(string obj)
        {
            var ti = new TicketTag { Name = Resources.NewTag };
            _workspace.Add(ti);
            Model.TicketTags.Add(ti);
            TicketTags.Add(new TicketTagViewModel(ti));
        }

        protected override string GetSaveErrorMessage()
        {
            if (PriceTags)
            {
                foreach (var ticketTag in TicketTags)
                {
                    try
                    {
                        var dec = Convert.ToDecimal(ticketTag.Model.Name);
                        if (dec.ToString() != ticketTag.Model.Name)
                            return Resources.NumericTagsShouldBeNumbersErrorMessage;
                    }
                    catch (Exception)
                    {
                        return Resources.NumericTagsShouldBeNumbersErrorMessage;
                    }
                }
            }

            if (NumericTags)
            {
                foreach (var ticketTag in TicketTags)
                {
                    try
                    {
                        var dec = Convert.ToInt32(ticketTag.Model.Name);
                        if (dec.ToString() != ticketTag.Model.Name)
                            return Resources.NumericTagsShouldBeNumbersErrorMessage;
                    }
                    catch (Exception)
                    {
                        return Resources.NumericTagsShouldBeNumbersErrorMessage;
                    }
                }
            }
            return base.GetSaveErrorMessage();
        }
    }
}
