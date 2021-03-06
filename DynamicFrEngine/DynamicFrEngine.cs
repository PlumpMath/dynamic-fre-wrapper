﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using FrEngineLoader.Properties;
using Microsoft.VisualBasic;

namespace FrEngineLoader
{
    /// <summary>
    ///     Loads and wraps ABBYY FineReader Engine 11 COM object so that it can be declared with "dynamic" keyword and
    ///     implements IDisposable pattern.
    /// </summary>
    public sealed partial class DynamicFrEngine : DynamicFrComObjectWrapper
    {
        private bool _disposed;

        /// <summary>
        ///     Initializes a new instance of wrapped ABBYY FineReader Engine 11 COM object using the specified loading
        ///     method.
        /// </summary>
        public DynamicFrEngine(FrEngineLoadingMethod loadingMethod, string projectId = null, string password = null,
            string frEngineDllPath = FrEngineUtils.FrEngineDllDefaultPath)
            : base(null)
        {
            _projectId = projectId;
            _password = password;

            switch (loadingMethod)
            {
                case FrEngineLoadingMethod.Native:
                    ComObject = LoadFrEngineNatively(frEngineDllPath);
                    break;
                case FrEngineLoadingMethod.InProcessComServer:
                    ComObject = LoadFrEngineAsComServer(FrEngineUtils.InprocComServerProgId);
                    break;
                case FrEngineLoadingMethod.OutOfProcessComServer:
                    ComObject = LoadFrEngineAsComServer(FrEngineUtils.OutprocComServerProgId);
                    break;
                default:
                    throw new ApplicationException(string.Format(Resources.EXC_LOADING_METHOD, loadingMethod));
            }

            ComObjectType = ComObject.GetType();
            NativeComObjectTypeName = Information.TypeName(ComObject);

            if (NativeComObjectTypeName == FrEngineUtils.DefaultComTypeNameFromVbInfo)
            {
                throw new ApplicationException(string.Format(Resources.EXC_FRE_DLL_NOT_REG,
                    FrEngineUtils.FrEngineDllFileName));
            }
        }

        public static IEnumerable<string> SupportedImageExtensions
        {
            get { return FrEngineUtils.SupportedImageExtensions; }
        }

        // Dispose pattern implementation for a derived class.
        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Cleanup managed resources.
                    base.Dispose(true); // Unload alive FrEngine COM object first.
                    UnloadFrEngineComServerIfLoaded();
                }
                // Cleanup unmanaged resources.
                UnloadFrEngineDllIfLoaded();
                _disposed = true;
            }
            base.Dispose(disposing);
        }

        private void UnloadFrEngineComServerIfLoaded()
        {
            if (_engineLoader == null) return;

            _engineLoader.ExplicitlyUnload();
            Marshal.FinalReleaseComObject(_engineLoader);
            _engineLoader = null;
        }

        private void UnloadFrEngineDllIfLoaded()
        {
            if (_frEngineDllHandle == IntPtr.Zero) return;

            _deinitializeEngine();
            FrEngineUtils.FreeLibrary(_frEngineDllHandle);
            _frEngineDllHandle = IntPtr.Zero;
        }
    }
}