using Newtonsoft.Json;
using Orchestrator.WebService.Client;
using Orchestrator.WebService.Client.OaaSClient;
using SMA.ISEAddon.Properties;
using SMA.ISEAddon.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Services.Client;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;

namespace SMA.ISEAddon.ViewModels
{
    public class SMAAuthoringViewModel : INotifyPropertyChanged
    {
        public class RunbookDefinition
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
        }

        public class RunbookParameterDefinition
        {
            public string Name { get; set; }
            public bool IsMandatory { get; set; }
            public string Type { get; set; }
            public string Value { get; set; }
        }

        private readonly Dictionary<string, Guid> activeRunbooks = new Dictionary<string, Guid>();
        private IList<RunbookParameterDefinition> runbookParameters;
        private IEnumerable<RunbookDefinition> runbooks;
        private RunbookDefinition activeRunbook;
        private OrchestratorApi api;
        private string webService;
        private bool isConnected;
        private string output;

        public event PropertyChangedEventHandler PropertyChanged;

        private DelegateCommand testRunbookCommand;
        private DelegateCommand publishRunbookCommand;
        private IAuthoringView View;

        public DelegateCommand TestRunbookCommand
        {
            get { return testRunbookCommand; }
            set { testRunbookCommand = value; InvokePropertyChanged("TestRunbookCommand"); }
        }

        public DelegateCommand PublishRunbookCommand
        {
            get { return publishRunbookCommand; }
            set { publishRunbookCommand = value; InvokePropertyChanged("PublishRunbookCommand"); }
        }

        public string WebService
        {
            get { return webService; }
            set { webService = value; InvokePropertyChanged("WebService"); }
        }

        public bool IsConnected
        {
            get { return isConnected; }
            set { isConnected = value; InvokePropertyChanged("IsConnected"); }
        }        

        public IEnumerable<RunbookDefinition> Runbooks
        {
            get { return runbooks; }
            set { runbooks = value; InvokePropertyChanged("Runbooks"); }
        }

        public RunbookDefinition ActiveRunbook
        {
            get { return activeRunbook; }
            set { 
                activeRunbook = value; 
                InvokePropertyChanged("ActiveRunbook"); 
                OpenRunbook();
                this.PublishRunbookCommand.InvokeCanExecuteChanged(); 
            }
        }     

        public IList<RunbookParameterDefinition> RunbookParameters
        {
            get { return runbookParameters; }
            set { runbookParameters = value; InvokePropertyChanged("RunbookParameters"); }
        }

        public SMAAuthoringViewModel(IAuthoringView view)
        {
            WebService = Settings.Default.WebServiceUri;
            TestRunbookCommand = new DelegateCommand(x => true, x => this.TestRunbook());
            PublishRunbookCommand = new DelegateCommand(x => this.ActiveRunbook != null && this.activeRunbooks.Any(y => y.Value == this.ActiveRunbook.Id), x => this.PublishRunbook());
            View = view;
            View.PropertyChanged += View_PropertyChanged;            
        }

        void View_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "ActiveFile")
            {
                Guid runbookId;
                if (this.activeRunbooks.TryGetValue(View.ActiveFile, out runbookId))
                {
                    var runbook = this.Runbooks.FirstOrDefault(x => x.Id == runbookId);
                    if (this.ActiveRunbook.Id != runbookId)
                        this.ActiveRunbook = runbook;
                }
            }
        }

        public string Output
        {
            get { return output; }
            set { output = value; InvokePropertyChanged("Output"); }
        }

        public void RefreshRunbooks()
        {
#if !DEBUG
            try
            {
#endif
            Connect();
            Runbooks = api.Runbooks.ToArray().Select(x => new RunbookDefinition { Id = x.RunbookID, Name = x.RunbookName }).ToArray();            
            AppendOutput("#### Runbook list loaded ####");
#if !DEBUG
            }
            catch (Exception ex)
            {
                MessageBox.Show(GetErrorMessage(ex), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
#endif

        }

        public void OpenRunbook()
        {
#if !DEBUG
            try
            {
#endif
            if (activeRunbook == null)
                return; 

            Connect();
    
            var runbookId = activeRunbook.Id;
            var runbook = api.Runbooks.Where(x => x.RunbookID == runbookId).First();
            var serviceRequestArgs1 = new DataServiceRequestArgs();
            serviceRequestArgs1.AcceptContentType = "application/octet-stream";
            var serviceRequestArgs2 = serviceRequestArgs1;
            var versionId = runbook.DraftRunbookVersionID == null ? runbook.Edit(api) : runbook.DraftRunbookVersionID;
            var runbookVersion = api.RunbookVersions.Where(x => x.RunbookVersionID == versionId).First();

            var parameters = api.RunbookParameters.Where(x => x.RunbookVersionID == runbook.DraftRunbookVersionID).ToArray();
            RunbookParameters = parameters.Select(x => new RunbookParameterDefinition
            {
                Name = x.Name,
                IsMandatory = x.IsMandatory,
                Type = x.Type,
                Value = string.Empty
            }).ToList();

            string content;
            using (DataServiceStreamResponse readStream = ((DataServiceContext)api).GetReadStream((object)runbookVersion, serviceRequestArgs2))
            {
                using (var streamReader = new StreamReader(readStream.Stream))
                    content = streamReader.ReadToEnd();
            }

            var tempFile = activeRunbooks.FirstOrDefault(x => x.Value == runbook.RunbookID).Key;
            if (string.IsNullOrEmpty(tempFile))
            {
                tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("D") + ".ps1");
                activeRunbooks[tempFile] = runbook.RunbookID;
            }

            File.WriteAllText(tempFile, content);            

            View.Open(tempFile);
#if !DEBUG
            }
            catch (Exception ex)
            {
                MessageBox.Show(GetErrorMessage(ex), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
#endif
        }

        public void TestRunbook()
        {
#if !DEBUG 
            try
            {
#endif
            Connect();
            View.Save();

            var runbook = ActiveRunbook == null ? null : api.Runbooks.Where(x => x.RunbookID == ActiveRunbook.Id).First();
            var path = View.ActiveFile;

            RunbookVersion runbookVersion;
            if (runbook == null)
            {
                runbookVersion = new RunbookVersion();
                runbookVersion.TenantID = new Guid("00000000-0000-0000-0000-000000000000");
                runbookVersion.IsDraft = true;
                api.AddToRunbookVersions(runbookVersion);
            }
            else
            {
                runbookVersion = api.RunbookVersions.Where(x => x.RunbookVersionID == runbook.DraftRunbookVersionID && x.IsDraft).FirstOrDefault();
            }

            var baseStream = new StreamReader(path, Encoding.UTF8).BaseStream;
            ((DataServiceContext)api).SetSaveStream(runbookVersion, baseStream, true, "application/octet-stream", string.Empty);
            var response = api.SaveChanges().FirstOrDefault() as ChangeOperationResponse;

            if (runbook == null && response != null)
            {
                api.Execute<RunbookVersion>(((EntityDescriptor)response.Descriptor).EditLink).Count();
                runbook = api.Runbooks.Where(x => x.RunbookID == runbookVersion.RunbookID).First();

                RefreshRunbooks();
                ActiveRunbook = Runbooks.First(x => x.Id == runbook.RunbookID);
                activeRunbooks[path] = runbook.RunbookID;
            }

            var parameters = RunbookParameters == null ? new List<NameValuePair>() : RunbookParameters.Where(x => !string.IsNullOrEmpty(x.Value)).Select(x => new NameValuePair { Name = x.Name, Value = SerializeValue(x) }).ToList();

            var result = runbook.TestRunbook(api, parameters);
            Job job;
            short? errors = 0;
            var exception = "";

            while (!(job = api.Jobs.Where(x => x.JobID == result).First()).EndTime.HasValue)
            {
                if (job.JobStatus == "Suspended")
                {
                    errors = job.ErrorCount;
                    exception = job.JobException;
                    job.Stop(api);
                }

                Thread.Sleep(1000);
            }

            if (job.JobStatus != "Stopped")
            {
                errors = job.ErrorCount;
                exception = job.JobException;
            }

            var output = QueryHelpers.GetJobOutput(api, job);

            var message = string.Format("#### Started: {0} - Finished: {1} - Status: {2} - Errors: {5} ####\r\nError: {4}\r\nOutput: {3}\r\n########\r\n", job.StartTime, job.EndTime, job.JobStatus, output, exception, errors);
            AppendOutput(message);
#if !DEBUG 
                }
                catch (Exception ex)
                {
                    MessageBox.Show(GetErrorMessage(ex), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
#endif
        }

        public void PublishRunbook()
        {
#if !DEBUG
            try
            {
#endif
            Connect();

            if (ActiveRunbook == null)
            {
                throw new InvalidOperationException("Active file is not associated to any runbooks.");
            }
            
            var runbook = api.Runbooks.Where(x => x.RunbookID == ActiveRunbook.Id).First();
            runbook.Publish(api);

            AppendOutput("#### Published ####");
#if !DEBUG
            }
            catch (Exception ex)
            {
                MessageBox.Show(GetErrorMessage(ex), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
#endif
        }

        public void Connect()
        {
            if (IsConnected)
                return;

            Settings.Default.WebServiceUri = WebService;
            Settings.Default.Save();

            api = new OrchestratorApi(new Uri(WebService));
            ((DataServiceContext)api).Timeout = 10;  
            ((DataServiceContext)api).Credentials = System.Net.CredentialCache.DefaultCredentials;
            api.MergeOption = MergeOption.OverwriteChanges;

            var runbookCount = api.Runbooks.Count();
            AppendOutput("#### Connected ####");

            IsConnected = true;
        }

        public void RemoveFile(string path)
        {
            activeRunbooks.Remove(path);
        }

        private void AppendOutput(string message)
        {
            Output = message + Output + "\r\n";
        }

        private void InvokePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private static string SerializeValue(RunbookParameterDefinition x)
        {
            string value = null;
            if (x.Type == "System.String[]")
            {
                value = Newtonsoft.Json.JsonConvert.SerializeObject((x.Value ?? string.Empty).Split(new[] { ',' }));
            }
            else
            {
                var convertedValue = Convert.ChangeType(x.Value, Type.GetType(x.Type));
                var settings = new JsonSerializerSettings
                {
                    DateFormatHandling = DateFormatHandling.MicrosoftDateFormat
                };
                value = JsonConvert.SerializeObject(convertedValue, settings);
            }
            return value;
        }

        private static string GetErrorMessage(Exception ex)
        {
            return ex.InnerException != null ? ex.InnerException.Message : ex.Message;
        }
    }
}
