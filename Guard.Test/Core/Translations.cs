using System.Text;
using System.Xml.Linq;
using SharpVectors.Dom;

namespace Guard.Test.Core
{
    public class Translations
    {
        private readonly List<XDocument> resources = [];
        private readonly string translationFolder = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "../../../../Guard.WPF/Resources/"
        );

        public Translations()
        {
            string[] paths = Directory.GetFiles(translationFolder, "Strings*.xaml");
            foreach (string path in paths)
            {
                var test = XDocument.Load(path);
                resources.Add(test);
            }
            Assert.NotEmpty(resources);
        }

        private static string GetXKey(XElement e)
        {
            return e.Attribute(
                    XName.Get("Key", "http://schemas.microsoft.com/winfx/2006/xaml")
                )?.Value ?? throw new Exception("Key not found");
        }

        [Fact]
        public void CheckKeys()
        {
            foreach (XDocument resource in resources)
            {
                Assert.NotNull(resource);
                Assert.NotNull(resource.Root);
                foreach (XElement element in resource.Root.Elements())
                {
                    Assert.NotNull(element);
                    Assert.Equal("String", element.Name.LocalName);
                    string key = GetXKey(element);
                    Assert.StartsWith("i.", key);
                    Assert.Matches("^[a-zA-Z0-9.]*$", key);
                }
            }
        }

        [Fact]
        public void CheckValues()
        {
            foreach (XDocument resource in resources)
            {
                Assert.NotNull(resource);
                Assert.NotNull(resource.Root);
                foreach (XElement element in resource.Root.Elements())
                {
                    Assert.NotNull(element);
                    Assert.Equal("String", element.Name.LocalName);
                    string? value = element.Value;
                    Assert.NotNull(value);
                    Assert.NotEmpty(value);
                }
            }
        }

        [Fact]
        public void CheckDuplicates()
        {
            foreach (XDocument resource in resources)
            {
                Assert.NotNull(resource);
                Assert.NotNull(resource.Root);
                HashSet<string> keys = [];
                foreach (XElement element in resource.Root.Elements())
                {
                    Assert.NotNull(element);
                    Assert.Equal("String", element.Name.LocalName);
                    string key = GetXKey(element);
                    Assert.DoesNotContain(key, keys);
                    keys.Add(key);
                }
            }
        }

        [Fact]
        public void CheckConsistency()
        {
            HashSet<string> keys = [];
            bool first = true;
            foreach (XDocument resource in resources)
            {
                Assert.NotNull(resource);
                Assert.NotNull(resource.Root);
                foreach (XElement element in resource.Root.Elements())
                {
                    Assert.NotNull(element);
                    Assert.Equal("String", element.Name.LocalName);

                    if (!first)
                    {
                        Assert.Contains(GetXKey(element), keys);
                    }
                    else
                    {
                        keys.Add(GetXKey(element));
                    }
                }
                first = false;
            }
        }
    }
}
