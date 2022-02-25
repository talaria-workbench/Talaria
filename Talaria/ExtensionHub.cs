using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;

namespace Talaria
{
    internal static class ExtensionHub
    {

        private static CompositionContainer? container;
        private static AggregateCatalog? catalog;

        public static void Import(object satisfyImports)
        {
            if (container is null) {

                catalog = new AggregateCatalog();
                container = new CompositionContainer(catalog);
                // TODO: Load assemblys dynamic from folder
                catalog.Catalogs.Add(new AssemblyCatalog(typeof(AddIn.Image.ImportImage).Assembly));
            }
            container.ComposeParts(satisfyImports);
        }

    }
}
