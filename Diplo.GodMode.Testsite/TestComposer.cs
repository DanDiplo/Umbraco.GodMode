using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace Diplo.GodMode.Testsite
{
    public class TestComposer : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
        {
            builder.Services.AddSingleton<IDiploFoo, DiploFoo>();
            builder.Services.AddKeyedSingleton<IDiploFoo, DiploFoo>("keyed");
        }
    }

    public interface IDiploFoo
    {
        string Foo();
    }

    public class DiploFoo : IDiploFoo
    {
        string IDiploFoo.Foo()
        {
            return "Diplo Foo to You!";
        }
    }
}
