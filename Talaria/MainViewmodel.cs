using Prism.Commands;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

using Windows.Storage.Pickers;

namespace Talaria
{
    internal partial class MainViewmodel
    {
        [SourceGenerators.AutoNotify]
        private ProjectViewmodel? project;

        public ICommand OpenCommand { get; }
        public ICommand CreateNewCommand { get; }

        public MainViewmodel()
        {
            this.OpenCommand = new DelegateCommand(async () =>
            {
                var dialog = new FileOpenPicker()
                {

                };
                dialog.FileTypeFilter.Add(".talaria");
                var result = await dialog.PickSingleFileAsync("open_Project");
                if (result is not null) {
                    this.Project = await ProjectViewmodel.Open(new FileInfo(result.Path));
                }
            });
            this.CreateNewCommand = new DelegateCommand(async () =>
            {
                var dialog = new FileSavePicker()
                {

                };
                dialog.DefaultFileExtension = ".talaria";
                var result = await dialog.PickSaveFileAsync();
                if (result is not null) {
                    this.Project = await ProjectViewmodel.Create(new FileInfo(result.Path));
                }
            });
        }
    }

    internal partial class ProjectViewmodel
    {
        public DirectoryInfo ProjectFolder { get; }

        private ProjectViewmodel(FileInfo projectFile)
        {
            if (!projectFile.Exists) {
                throw new ArgumentException("Project does not exists");
            }

            if (projectFile.Directory is null) {
                throw new ArgumentException("Projectfile can't be in root");
            }
            this.ProjectFolder = projectFile.Directory;

        }

        public static Task<ProjectViewmodel> Open(FileInfo projectFile)
        {
            return Task.FromResult(new ProjectViewmodel(projectFile));
        }
        public static async Task<ProjectViewmodel> Create(FileInfo projectFile)
        {
            if (projectFile.Exists) {
                throw new ArgumentException("Project already Exists");
            }
            using (var writer = projectFile.AppendText()) {
                await writer.WriteLineAsync("<project />");
            }
            return new ProjectViewmodel(projectFile);
        }
    }

}
