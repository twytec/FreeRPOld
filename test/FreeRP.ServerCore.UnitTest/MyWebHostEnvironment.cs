using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;

namespace FreeRP.ServerCore.UnitTest
{
    internal class MyWebHostEnvironment : IWebHostEnvironment
    {
        string _webRoot = AppDomain.CurrentDomain.BaseDirectory;
        public string WebRootPath { get => _webRoot; set => _webRoot = value; }
        public IFileProvider WebRootFileProvider { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string ApplicationName { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public IFileProvider ContentRootFileProvider { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string ContentRootPath { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string EnvironmentName { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    }
}
