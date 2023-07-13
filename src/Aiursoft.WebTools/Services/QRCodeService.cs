using Aiursoft.Scanner.Abstractions;
using Aiursoft.CSTools.Tools;
using Net.Codecrete.QrCodeGenerator;

namespace Aiursoft.WebTools.Services;

public class QRCodeService : ITransientDependency
{
    public string ToQRCodeSvgXml(string source)
    {
        var qr = QrCode.EncodeText(source, QrCode.Ecc.Medium);
        return qr.ToSvgString(0);
    }

    public string ToQRCodeImgSrc(string source)
    {
        return "data:image/svg+xml;base64," + ToQRCodeSvgXml(source).StringToBase64();
    }
}