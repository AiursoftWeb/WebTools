using Aiursoft.WebTools.Services;

namespace Aiursoft.WebTools.Tests.Services;

[TestClass]
public class TestQRCodeService
{
    [TestMethod]
    public void HelloWorldTest()
    {
        var service = new QRCodeService();
        var qrCode = service.ToQRCodeImgSrc("https://www.google.com");
        Assert.StartsWith("data:image/svg+xml;base64,", qrCode);
    }
}
