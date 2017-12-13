Typed DataSet Generator for the Global Namespace
================================================

[![Build Status](https://img.shields.io/appveyor/ci/kevinoid/kevinlocke-visualstudio-globaldatasetgenerators/master.svg?style=flat&label=build+status)](https://ci.appveyor.com/project/kevinoid/kevinlocke-visualstudio-globaldatasetgenerators)

This is a project to build a Visual Studio Extension (VSIX) for a [Single File
Generator](https://docs.microsoft.com/en-us/visualstudio/extensibility/internals/implementing-single-file-generators)
which wraps the bundled MSDataSetGenerator for generating [Strongly Typed
DataSets](https://docs.microsoft.com/en-us/dotnet/framework/data/adonet/dataset-datatable-dataview/typed-datasets)
to add support for generating `DataSet`s in the global (i.e. default, unnamed)
namespace.

## Usage

To use the Single File Generator from this project:

1. Install the [latest build of the Visual Studio
  extension](https://ci.appveyor.com/api/projects/kevinoid/kevinlocke-visualstudio-globaldatasetgenerators/artifacts/bin%2FRelease%2FKevinLocke.VisualStudio.GlobalDataSetGenerators.vsix)
2. Open a project and select a DataSet XSD file in the Solution Explorer.
3. In Properties, change "Custom Tool" to "GlobalNamespaceDataSetGenerator".
   ("Custom Tool Namespace" is ignored and can be set to any value.)
4. Build the project or right-click on the XSD file and select "Run Custom
   Tool".

## Why not use MSDataSetGenerator directly?

There are at least two problems using MSDataSetGenerator in the global
namespace:

1.  *It can't easily be configured.*  When the "Custom Tool Namespace" is
    empty, Visual Studio provides a default value.  First from the Project
    default, then from the directory name.  The project default can't be
    changed to empty in the designer (It yields "Root Namespace: The entered
    value for the property 'Default Namespace' is invalid.") but it can be set
    by editing the .csproj file directly.
2.  *It generates incorrect code.*  Even if the above difficulties are
    overcome and the default namespace is passed to MSDataSetGenerator, it
    generates incorrect code due to the addition of `#pragma warning disable
    1591` by replacing the first occurrance of `"namespace"`.  So if a
    namespace declaration is not generated because the DataSet is being
    generated for the global namespace, the first occurrance of `"namespace"`
    will get clobbered, usually resulting in compilation failures due to an
    invalid string literal.

## Why use the global namespace?

Although using the global namespace is generally discouraged for a variety of
reasons, sometimes it may be preferable.  The reason that motivated this
project is migrating a Web Site Project which contained Typed DataSets in the
global namespace to a Web Application Project where this error was
encountered.  Moving the DataSets into namespaces is planned, but not
currently practical.  Backwards-compatibility can also be a concern.
