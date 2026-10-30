using Microsoft.Extensions.DependencyInjection;
using StatementApp.Core.Abstractions;
using StatementApp.Core.Validation;
using StatementApp.Data.DocumentGeneration;
using StatementApp.Data.Persistence;
using StatementApp.UI.Presenters;
using StatementApp.UI.Views;

namespace StatementApp.UI
{
    internal static class Program
    {
     
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            var services = new ServiceCollection();

            services.AddSingleton<IMainView, MainForm>();
            services.AddTransient<MainPresenter>();
            services.AddTransient<DirectoriesControl>();                                                 
            services.AddSingleton<JsonRepository>();
            services.AddSingleton<IStatementRepository>(sp => sp.GetRequiredService<JsonRepository>()); 
            services.AddSingleton<IStatementExporter, DocTemplateGenerator>();                           
            services.AddSingleton<IStatementValidator>(_ => ValidatorFactory.Create());

            try
            {
                var provider = services.BuildServiceProvider();
                var presenter = provider.GetRequiredService<MainPresenter>();
                var form = (MainForm)provider.GetRequiredService<IMainView>();

                Application.Run(form);
                GC.KeepAlive(presenter);
            }
            catch (InvalidDataException ex)
            {
               
                MessageBox.Show(ex.Message, "Не удалось запустить", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
