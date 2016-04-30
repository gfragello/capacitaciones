using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(Cursos.Startup))]
namespace Cursos
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
