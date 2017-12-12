// <copyright file="GlobalDataSetGenerator.cs" company="Kevin Locke">
// Copyright 2017 Kevin Locke &lt;kevin@kevinlocke.name&gt;
// This file is part of KevinLocke.VisualStudio.GlobalDataSetGenerators,
// publicly available under the MIT License.  See LICENSE.txt for details.
// </copyright>

namespace KevinLocke.VisualStudio.GlobalDataSetGenerators
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Security.Permissions;
    using System.Text;
    using System.Text.RegularExpressions;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.OLE.Interop;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;
    using Microsoft.Win32;
    using VSLangProj80;

    /// <summary>
    /// An <see cref="IVsSingleFileGenerator"/> for generating a Typed DataSet
    /// from an .xsd, like MSDataSetGenerator, except that the DataSet is
    /// always generated in the global namespace.
    /// </summary>
    /// <remarks>
    /// Implementation guidelines:
    /// https://stackoverflow.com/a/42842407/503410
    /// https://github.com/Microsoft/VSSDK-Extensibility-Samples/blob/master/Single_File_Generator/C%23/XmlClassGenerator.cs
    /// </remarks>
    [CLSCompliant(false)]
    [CodeGeneratorRegistration(
        typeof(GlobalDataSetGenerator),
        "Wrapper for the Microsoft C# Code Generator for XSD which generates to the global namespace",
        vsContextGuids.vsContextGuidVCSProject,
        GeneratesDesignTimeSource = true,
        GeneratorRegKeyName = "GlobalDataSetGenerator")]
    [ComVisible(true)]
    [Guid("920DAD23-09C9-4AFD-BDC9-55405D587AD6")]
    [ProvideObject(
        typeof(GlobalDataSetGenerator),
        RegisterUsing = RegistrationMethod.CodeBase)]
    public class GlobalDataSetGenerator : IVsSingleFileGenerator, IVsRefactorNotify, IObjectWithSite
    {
        private const string MSDataSetGeneratorGuid = "{E76D53CC-3D4F-40a2-BD4D-4F3419755476}";
        private const string TemporaryNamespaceName = "GlobalDataSetGeneratorTempNamespace";

        // FIXME: Regex assumes top-level namespaces start and end at the
        // beginning of a line and no "}" within the namespace does.
        private static readonly Regex CSharpNamespace = new Regex(
            @"^namespace\s+" + TemporaryNamespaceName + @"(?:\.(\S+))?\s*\{(.*?)^\}",
            RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.Singleline);

        private readonly IVsSingleFileGenerator msDataSetGenerator;
        private readonly IVsRefactorNotify msDataSetGeneratorRN;
        private readonly IObjectWithSite msDataSetGeneratorSite;

        /// <summary>
        /// Initializes a new instance of the
        /// <see cref="GlobalDataSetGenerator"/> class.
        /// </summary>
        public GlobalDataSetGenerator()
        {
            // FIXME: Should use Microsoft.VisualStudio.Shell.ServiceProvider
            // from SetSite to instantiate?  Like
            // https://github.com/Microsoft/VSSDK-Extensibility-Samples/blob/60803c0/WPFDesigner_XML/WPFDesigner_XML/EditorFactory.cs#L51-L60
            this.msDataSetGenerator = GetGeneratorByClsid(MSDataSetGeneratorGuid);
            this.msDataSetGeneratorRN = (IVsRefactorNotify)this.msDataSetGenerator;
            this.msDataSetGeneratorSite = (IObjectWithSite)this.msDataSetGenerator;
        }

        /// <summary>
        /// Retrieves the file extension that is given to the output file name.
        /// </summary>
        /// <param name="pbstrDefaultExtension">The file extension that is to
        /// be given to the output file name.  (With a leading period.)</param>
        /// <returns><c>VSConstants.S_OK</c> or an error code.</returns>
        public int DefaultExtension(out string pbstrDefaultExtension)
        {
            return this.msDataSetGenerator.DefaultExtension(out pbstrDefaultExtension);
        }

        /// <summary>
        /// Executes the transformation and returns the newly generated output file.
        /// </summary>
        /// <param name="wszInputFilePath">The full path of the input file.</param>
        /// <param name="bstrInputFileContents">The contents of the input file.</param>
        /// <param name="wszDefaultNamespace">Unused parameter.  Output is
        /// always in the global (i.e. default, unnamed) namespace.</param>
        /// <param name="rgbOutputFileContents">Returns an array of bytes to
        /// be written to the generated file.</param>
        /// <param name="pcbOutput">Returns the count of bytes in the
        /// rgbOutputFileContent array.</param>
        /// <param name="pGenerateProgress">Interface through which the
        /// generator can report its progress to the project system.</param>
        /// <returns><see cref="VSConstants.S_OK"/> or an error code.</returns>
        [SecurityPermissionAttribute(SecurityAction.Demand, UnmanagedCode = true)]
        public int Generate(
            string wszInputFilePath,
            string bstrInputFileContents,
            string wszDefaultNamespace,
            IntPtr[] rgbOutputFileContents,
            out uint pcbOutput,
            IVsGeneratorProgress pGenerateProgress)
        {
            int status = this.msDataSetGenerator.Generate(
                wszInputFilePath,
                bstrInputFileContents,
                TemporaryNamespaceName,
                rgbOutputFileContents,
                out pcbOutput,
                pGenerateProgress);
            if (status != VSConstants.S_OK ||
                pcbOutput == 0 ||
                rgbOutputFileContents == null ||
                rgbOutputFileContents[0] == null)
            {
                return status;
            }

            byte[] origBytes = new byte[pcbOutput];
            Marshal.Copy(rgbOutputFileContents[0], origBytes, 0, checked((int)pcbOutput));
            string origContents = BytesToString(origBytes, out Encoding contentEncoding);

            string newContents = RemoveTemporaryNamespace(origContents);
            byte[] newBytes = contentEncoding.GetBytes(newContents);
            Debug.Assert(
                newBytes.Length < origBytes.Length,
                "Content larger after removing namespace!?");
            Marshal.Copy(newBytes, 0, rgbOutputFileContents[0], newBytes.Length);
            pcbOutput = (uint)newBytes.Length;
            return VSConstants.S_OK;
        }

#pragma warning disable SA1611 // Element parameters must be documented
#pragma warning disable SA1615 // Element return value must be documented

        /// <summary>Forwards to MSDataSetGenerator.</summary>
        /// <seealso cref="IObjectWithSite.GetSite(ref Guid, out IntPtr)"/>
        public void GetSite(ref Guid riid, out IntPtr ppvSite)
        {
            this.msDataSetGeneratorSite.GetSite(riid, out ppvSite);
        }

        /// <summary>Forwards to MSDataSetGenerator.</summary>
        /// <seealso cref="IObjectWithSite.SetSite(object)"/>
        public void SetSite(object pUnkSite)
        {
            this.msDataSetGeneratorSite.SetSite(pUnkSite);
        }

        /// <summary>Forwards to MSDataSetGenerator.</summary>
        /// <seealso cref="IVsRefactorNotify.OnBeforeGlobalSymbolRenamed(IVsHierarchy, uint, uint, string[], string, out Array)"/>
        public int OnBeforeGlobalSymbolRenamed(
            IVsHierarchy pHier,
            uint itemid,
            uint cRQNames,
            string[] rglpszRQName,
            string lpszNewName,
            out Array prgAdditionalCheckoutVSITEMIDs)
        {
            return this.msDataSetGeneratorRN.OnBeforeGlobalSymbolRenamed(
                pHier,
                itemid,
                cRQNames,
                rglpszRQName,
                lpszNewName,
                out prgAdditionalCheckoutVSITEMIDs);
        }

        /// <summary>Forwards to MSDataSetGenerator.</summary>
        /// <seealso cref="IVsRefactorNotify.OnGlobalSymbolRenamed(IVsHierarchy, uint, uint, string[], string)"/>
        public int OnGlobalSymbolRenamed(
            IVsHierarchy pHier,
            uint itemid,
            uint cRQNames,
            string[] rglpszRQName,
            string lpszNewName)
        {
            return this.msDataSetGeneratorRN.OnGlobalSymbolRenamed(
                pHier,
                itemid,
                cRQNames,
                rglpszRQName,
                lpszNewName);
        }

        /// <summary>Forwards to MSDataSetGenerator.</summary>
        /// <seealso cref="IVsRefactorNotify.OnBeforeReorderParams(IVsHierarchy, uint, string, uint, uint[], out Array)"/>
        public int OnBeforeReorderParams(
            IVsHierarchy pHier,
            uint itemid,
            string lpszRQName,
            uint cParamIndexes,
            uint[] rgParamIndexes,
            out Array prgAdditionalCheckoutVSITEMIDs)
        {
            return this.msDataSetGeneratorRN.OnBeforeReorderParams(
                pHier,
                itemid,
                lpszRQName,
                cParamIndexes,
                rgParamIndexes,
                out prgAdditionalCheckoutVSITEMIDs);
        }

        /// <summary>Forwards to MSDataSetGenerator.</summary>
        /// <seealso cref="IVsRefactorNotify.OnReorderParams(IVsHierarchy, uint, string, uint, uint[])"/>
        public int OnReorderParams(
            IVsHierarchy pHier,
            uint itemid,
            string lpszRQName,
            uint cParamIndexes,
            uint[] rgParamIndexes)
        {
            return this.msDataSetGeneratorRN.OnReorderParams(
                pHier,
                itemid,
                lpszRQName,
                cParamIndexes,
                rgParamIndexes);
        }

        /// <summary>Forwards to MSDataSetGenerator.</summary>
        /// <seealso cref="IVsRefactorNotify.OnBeforeRemoveParams(IVsHierarchy, uint, string, uint, uint[], out Array)"/>
        public int OnBeforeRemoveParams(
            IVsHierarchy pHier,
            uint itemid,
            string lpszRQName,
            uint cParamIndexes,
            uint[] rgParamIndexes,
            out Array prgAdditionalCheckoutVSITEMIDs)
        {
            return this.msDataSetGeneratorRN.OnBeforeRemoveParams(
                pHier,
                itemid,
                lpszRQName,
                cParamIndexes,
                rgParamIndexes,
                out prgAdditionalCheckoutVSITEMIDs);
        }

        /// <summary>Forwards to MSDataSetGenerator.</summary>
        /// <seealso cref="IVsRefactorNotify.OnRemoveParams(IVsHierarchy, uint, string, uint, uint[])"/>
        public int OnRemoveParams(
            IVsHierarchy pHier,
            uint itemid,
            string lpszRQName,
            uint cParamIndexes,
            uint[] rgParamIndexes)
        {
            return this.msDataSetGeneratorRN.OnRemoveParams(
                pHier,
                itemid,
                lpszRQName,
                cParamIndexes,
                rgParamIndexes);
        }

        /// <summary>Forwards to MSDataSetGenerator.</summary>
        /// <seealso cref="IVsRefactorNotify.OnBeforeAddParams(IVsHierarchy, uint, string, uint, uint[], string[], string[], out Array)"/>
        public int OnBeforeAddParams(
            IVsHierarchy pHier,
            uint itemid,
            string lpszRQName,
            uint cParams,
            uint[] rgszParamIndexes,
            string[] rgszRQTypeNames,
            string[] rgszParamNames,
            out Array prgAdditionalCheckoutVSITEMIDs)
        {
            return this.msDataSetGeneratorRN.OnBeforeAddParams(
                pHier,
                itemid,
                lpszRQName,
                cParams,
                rgszParamIndexes,
                rgszRQTypeNames,
                rgszParamNames,
                out prgAdditionalCheckoutVSITEMIDs);
        }

        /// <summary>Forwards to MSDataSetGenerator.</summary>
        /// <seealso cref="IVsRefactorNotify.OnAddParams(IVsHierarchy, uint, string, uint, uint[], string[], string[])"/>
        public int OnAddParams(
            IVsHierarchy pHier,
            uint itemid,
            string lpszRQName,
            uint cParams,
            uint[] rgszParamIndexes,
            string[] rgszRQTypeNames,
            string[] rgszParamNames)
        {
            return this.msDataSetGeneratorRN.OnAddParams(
                pHier,
                itemid,
                lpszRQName,
                cParams,
                rgszParamIndexes,
                rgszRQTypeNames,
                rgszParamNames);
        }

#pragma warning restore SA1615 // Element return value must be documented
#pragma warning restore SA1611 // Element parameters must be documented

        /// <summary>
        /// Converts an array of bytes to a string based on BOM or as UTF-8.
        /// https://stackoverflow.com/a/32677319/503410
        /// </summary>
        /// <param name="bytes">Byte array to convert.</param>
        /// <param name="encoding">Returns the encoding used to decode
        /// <paramref name="bytes"/></param>
        /// <returns><paramref name="bytes"/> converted to string.</returns>
        private static string BytesToString(byte[] bytes, out Encoding encoding)
        {
            MemoryStream stream = null;
            try
            {
                stream = new MemoryStream(bytes);
                using (StreamReader reader = new StreamReader(stream))
                {
                    stream = null;
                    encoding = reader.CurrentEncoding;
                    return reader.ReadToEnd();
                }
            }
            finally
            {
                if (stream != null)
                {
                    stream.Dispose();
                }
            }
        }

        /// <summary>
        /// Instantiates an <see cref="IVsSingleFileGenerator"/> by CLSID.
        /// </summary>
        /// <param name="clsid">CLSID of the class to instantiate.</param>
        /// <returns>An instance of a class with the given CLSID.</returns>
        /// <exception cref="ArgumentException">If <paramref name="clsid"/>
        /// could not be found.</exception>
        private static IVsSingleFileGenerator GetGeneratorByClsid(string clsid)
        {
            using (RegistryKey visualStudioKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\VisualStudio"))
            {
                SortedDictionary<decimal, string> subKeyByNum = new SortedDictionary<decimal, string>();
                foreach (string subKeyName in visualStudioKey.GetSubKeyNames())
                {
                    if (decimal.TryParse(subKeyName, out decimal version))
                    {
                        subKeyByNum.Add(version, subKeyName);
                    }
                }

                // Search for CLSID for most recent version of Visual Studio
                foreach (string version in subKeyByNum.Values)
                {
                    string msDataSetGeneratorKeyPath = string.Format(CultureInfo.InvariantCulture, @"{0}\CLSID\{1}", version, clsid);
                    using (RegistryKey msDataSetGeneratorKey = visualStudioKey.OpenSubKey(msDataSetGeneratorKeyPath))
                    {
                        if (msDataSetGeneratorKey != null)
                        {
                            string assemblyName = (string)msDataSetGeneratorKey.GetValue("Assembly");
                            string className = (string)msDataSetGeneratorKey.GetValue("Class");
                            return (IVsSingleFileGenerator)Activator.CreateInstance(assemblyName, className).Unwrap();
                        }
                    }
                }
            }

            throw new ArgumentException("Unable to find CLSID " + clsid, "clsid");
        }

        /// <summary>
        /// Removes namespace named <see cref="TemporaryNamespaceName"/> from a
        /// given block of code.
        /// </summary>
        /// <param name="code">C# code in which to remove namespace
        /// <see cref="TemporaryNamespaceName"/>.</param>
        /// <returns><paramref name="code"/> with namespace
        /// <see cref="TemporaryNamespaceName"/> removed such that code in
        /// that namespace is now in the global namespace.</returns>
        private static string RemoveTemporaryNamespace(string code)
        {
            // TODO: Properly parse the C# code and operate on parse tree.
            // https://stackoverflow.com/q/81406/503410
            return CSharpNamespace.Replace(code, (Match m) =>
            {
                string innerNamespaceName = m.Groups[1].Value;
                string namespaceCode = m.Groups[2].Value;
                if (string.IsNullOrEmpty(innerNamespaceName))
                {
                    // Move to global namespace.  Remove namespace declaration.
                    return namespaceCode;
                }

                return "namespace " + innerNamespaceName + " {" + namespaceCode + "}";
            });
        }
    }
}
