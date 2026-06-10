using System.IO;
using DotNetEnv;
using Syncfusion.Licensing;

namespace MMS.WpfApp;

public partial class App
{
    public App() 
    {
        Env.Load(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..\..\.env")));
        
        SyncfusionLicenseProvider.RegisterLicense(Env.GetString("SYNCFUSION_LICENSE_KEY"));
        
        InitializeComponent();
    }
}