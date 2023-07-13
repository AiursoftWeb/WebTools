using Aiursoft.WebTools.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Aiursoft.WebTools.Tests.Services;

[TestClass]
public class TestQRCodeService
{
    [TestMethod]
    public void HelloWorldTest()
    {
        var service = new QRCodeService();
        var qrCode = service.ToQRCodeImgSrc("https://www.google.com");
        Assert.IsTrue(qrCode.StartsWith("data:image/svg+xml;base64,"));
    }
}
