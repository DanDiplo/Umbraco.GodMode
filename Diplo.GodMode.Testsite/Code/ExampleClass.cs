using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Diplo.GodMode.Testsite.Code
{
    public interface IExampleClass
    {
        public bool ThisIsCool { get; }
    }

    public class ExampleClass : IExampleClass
    {
        public bool ThisIsCool => true;
    }

    [HtmlTargetElement("email", TagStructure = TagStructure.WithoutEndTag)]
    public class EmailTagHelper : TagHelper
    {
        private const string EmailDomain = "contoso.com";
        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = "a";
            var content = await output.GetChildContentAsync();
            var target = content.GetContent() + "@" + EmailDomain;
            output.Attributes.SetAttribute("href", "mailto:" + target);
            output.Content.SetContent(target);
        }
    }

    public class TestController : ControllerBase
    {

    }
}

