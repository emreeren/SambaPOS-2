using System;
using System.Collections.ObjectModel;
using System.Linq;
using Samba.Domain.Models.Inventory;
using Samba.Infrastructure.Data;
using Samba.Localization.Properties;
using Samba.Presentation.Common;
using Samba.Presentation.Common.ModelBase;
using Samba.Services;

namespace Samba.Modules.InventoryModule
{
    class TransactionViewModel : EntityViewModelBase<Transaction>
    {
        private IWorkspace _workspace;
        public TransactionViewModel(Transaction model)
            : base(model)
        {
            AddTransactionItemCommand = new CaptionCommand<string>(string.Format(Resources.Add_f, Resources.Line), OnAddTransactionItem, CanAddTransactionItem);
            DeleteTransactionItemCommand = new CaptionCommand<string>(string.Format(Resources.Delete_f, Resources.Line), OnDeleteTransactionItem, CanDeleteTransactionItem);
        }

        public DateTime Date
        {
            get { return Model.Date; }
            set { Model.Date = value; }
        }

        public string DateLabel { get { return string.Format(Resources.DocumentDate_f, Date); } }
        public string TimeLabel { get { return string.Format(Resources.DocumentTime_f, Date); } }

        public ICaptionCommand AddTransactionItemCommand { get; set; }
        public ICaptionCommand DeleteTransactionItemCommand { get; set; }

        private ObservableCollection<TransactionItemViewModel> _transactionItems;
        public ObservableCollection<TransactionItemViewModel> TransactionItems
        {
            get { return _transactionItems ?? (_transactionItems = GetTransactionItems()); }
        }

        private ObservableCollection<TransactionItemViewModel> GetTransactionItems()
        {
            if (Model.TransactionItems.Count == 0)
                AddTransactionItemCommand.Execute("");
            return new ObservableCollection<TransactionItemViewModel>(
                     Model.TransactionItems.Select(x => new TransactionItemViewModel(x, _workspace)));
        }

        private TransactionItemViewModel _selectedTransactionItem;
        public TransactionItemViewModel SelectedTransactionItem
        {
            get { return _selectedTransactionItem; }
            set
            {
                _selectedTransactionItem = value;
                RaisePropertyChanged("SelectedTransactionItem");
            }
        }

        private bool CanDeleteTransactionItem(string arg)
        {
            return SelectedTransactionItem != null;
        }

        private void OnDeleteTransactionItem(string obj)
        {
            if (SelectedTransactionItem.Model.Id > 0)
                _workspace.Delete(SelectedTransactionItem.Model);
            Model.TransactionItems.Remove(SelectedTransactionItem.Model);
            TransactionItems.Remove(SelectedTransactionItem);
        }

        private bool CanAddTransactionItem(string arg)
        {
            return true;
        }

        protected override bool CanSave(string arg)
        {
            return AppServices.MainDataContext.IsCurrentWorkPeriodOpen && base.CanSave(arg);
        }

        private void OnAddTransactionItem(string obj)
        {
            var ti = new TransactionItem();
            var tiv = new TransactionItemViewModel(ti, _workspace);
            Model.TransactionItems.Add(ti);
            TransactionItems.Add(tiv);
            SelectedTransactionItem = tiv;
        }

        protected override void Initialize(IWorkspace workspace)
        {
            _workspace = workspace;
        }

        protected override void OnSave(string value)
        {
            var modified = false;
            foreach (var transactionItemViewModel in _transactionItems)
            {
                if (transactionItemViewModel.Model.InventoryItem == null || transactionItemViewModel.Quantity == 0)
                {
                    modified = true;
                    Model.TransactionItems.Remove(transactionItemViewModel.Model);
                    if (transactionItemViewModel.Model.Id > 0)
                        _workspace.Delete(transactionItemViewModel.Model);
                }
            }
            if (modified) _transactionItems = null;
            base.OnSave(value);
        }

        public override Type GetViewType()
        {
            return typeof(TransactionView);
        }

        public override string GetModelTypeString()
        {
            return Resources.TransactionDocument;
        }
    }
}
