using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows;
using Microsoft.Practices.EnterpriseLibrary.Common.Configuration;
using Microsoft.Practices.EnterpriseLibrary.Validation;
using Samba.Infrastructure.Data;
using Samba.Localization.Properties;

namespace Samba.Presentation.Common.ModelBase
{
    public abstract class EntityViewModelBase<TModel> : VisibleViewModelBase where TModel : class, IEntity
    {
        public TModel Model { get; private set; }
        public ICaptionCommand SaveCommand { get; private set; }
        private readonly ValidatorFactory _validatorFactory;
        private IWorkspace _workspace;

        protected EntityViewModelBase(TModel model)
        {
            Model = model;
            SaveCommand = new CaptionCommand<string>(Resources.Save, OnSave, CanSave);
            _validatorFactory = EnterpriseLibraryContainer.Current.GetInstance<ValidatorFactory>();
        }

        public string Name
        {
            get { return Model.Name; }
            set { Model.Name = value.Trim(); RaisePropertyChanged("Name"); }
        }

        private string _error;
        private bool _modelSaved;
        public string Error { get { return _error; } set { _error = value; RaisePropertyChanged("Error"); } }

        public abstract string GetModelTypeString();

        public void Init(IWorkspace workspace)
        {
            _modelSaved = false;
            _workspace = workspace;
            Initialize(workspace);
        }

        protected virtual void Initialize(IWorkspace workspace)
        {

        }

        public override void OnShown()
        {
            _modelSaved = false;
        }

        public override void OnClosed()
        {
            if (!_modelSaved)
                RollbackModel();
        }

        protected override string GetHeaderInfo()
        {
            if (Model.Id > 0)
                return string.Format(Resources.EditModel_f, GetModelTypeString(), Name);
            return string.Format(Resources.AddModel_f, GetModelTypeString());
        }

        protected virtual void OnSave(string value)
        {
            var errorMessage = GetSaveErrorMessage();
            if (CanSave())
            {
                _modelSaved = true;
                if (Model.Id == 0)
                {
                    this.PublishEvent(EventTopicNames.AddedModelSaved);
                }
                this.PublishEvent(EventTopicNames.ModelAddedOrDeleted);
                ((VisibleViewModelBase)this).PublishEvent(EventTopicNames.ViewClosed);
            }
            else
            {
                if (string.IsNullOrEmpty(Name))
                    errorMessage = string.Format(Resources.EmptyNameError, GetModelTypeString());
                MessageBox.Show(errorMessage, Resources.CantSave);
            }
        }

        public bool CanSave()
        {
            return !string.IsNullOrEmpty(Name) && string.IsNullOrEmpty(GetSaveErrorMessage()) && CanSave("");
        }

        protected virtual string GetSaveErrorMessage()
        {
            return "";
        }

        protected virtual bool CanSave(string arg)
        {
            return Validate();
        }

        private bool Validate()
        {
            var results = _validatorFactory.CreateValidator(typeof(TModel)).Validate(Model);
            Error = GetErrors(results);
            return results.IsValid;
        }

        private static string GetErrors(IEnumerable<ValidationResult> results)
        {
            var builder = new StringBuilder();
            foreach (var result in results)
            {
                builder.AppendLine(
                    string.Format(
                       CultureInfo.CurrentCulture,
                       "* {0}",
                       result.Message));
            }
            return builder.ToString();
        }

        public void RollbackModel()
        {
            if (Model.Id > 0)
            {
                _workspace.Refresh(Model);
                RaisePropertyChanged("Name");
            }
        }
    }
}
