using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.AspNetCore.Http;
using Moq;

namespace Aiursoft.WebTools.Tests
{
    [TestClass]
    public class ExtendsTests
    {
        [TestMethod]
        public void IsMobileBrowser_ValidUserAgent_ReturnsTrue()
        {
            // Arrange
            var userAgent = "Mozilla/5.0 (Linux; Android 10; SM-G975F) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/89.0.4389.105 Mobile Safari/537.36";
            var request = new Mock<HttpRequest>();
            request.Setup(r => r.Headers["User-Agent"]).Returns(userAgent);

            // Act
            var result = request.Object.IsMobileBrowser();

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void IsMobileBrowser_InvalidUserAgent_ReturnsFalse()
        {
            // Arrange
            var userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/89.0.4389.105 Safari/537.36";
            var request = new Mock<HttpRequest>();
            request.Setup(r => r.Headers["User-Agent"]).Returns(userAgent);

            // Act
            var result = request.Object.IsMobileBrowser();

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void IsWeChat_ValidUserAgent_ReturnsTrue()
        {
            // Arrange
            var request = new Mock<HttpRequest>();
            request.Setup(r => r.Headers["User-Agent"]).Returns("Mozilla/5.0 (iPhone; CPU iPhone OS 13_3 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Mobile/15E148 MicroMessenger/7.0.12(0x17000C21) NetType/WIFI Language/zh_CN");

            // Act
            bool result = request.Object.IsWeChat();

            // Assert
            Assert.IsTrue(result);
        }


        [TestMethod]
        public void IsWeChat_InvalidUserAgent_ReturnsFalse()
        {
            // Arrange
            var userAgent = "Mozilla/5.0 (Linux; Android 10; SM-G975F) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/89.0.4389.105 Mobile Safari/537.36";
            var request = new Mock<HttpRequest>();
            request.Setup(r => r.Headers["User-Agent"]).Returns(userAgent);

            // Act
            var result = request.Object.IsWeChat();

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void IsIos_ValidUserAgent_ReturnsTrue()
        {
            // Arrange
            var userAgent = "Mozilla/5.0 (iPhone; CPU iPhone OS 14_6 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/14.1.1 Mobile/15E148 Safari/604.1";
            var request = new Mock<HttpRequest>();
            request.Setup(r => r.Headers["User-Agent"]).Returns(userAgent);

            // Act
            var result = request.Object.IsIos();

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void IsIos_InvalidUserAgent_ReturnsFalse()
        {
            // Arrange
            var userAgent = "Mozilla/5.0 (Linux; Android 10; SM-G975F) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/89.0.4389.105 Mobile Safari/537.36";
            var request = new Mock<HttpRequest>();
            request.Setup(r => r.Headers["User-Agent"]).Returns(userAgent);

            // Act
            var result = request.Object.IsIos();

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void IsAndroid_ValidUserAgent_ReturnsTrue()
        {
            // Arrange
            var userAgent = "Mozilla/5.0 (Linux; Android 10; SM-G975F) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/89.0.4389.105 Mobile Safari/537.36";
            var request = new Mock<HttpRequest>();
            request.Setup(r => r.Headers["User-Agent"]).Returns(userAgent);

            // Act
            var result = request.Object.IsAndroid();

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void IsAndroid_InvalidUserAgent_ReturnsFalse()
        {
            // Arrange
            var userAgent = "Mozilla/5.0 (iPhone; CPU iPhone OS 14_6 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/14.1.1 Mobile/15E148 Safari/604.1";
            var request = new Mock<HttpRequest>();
            request.Setup(r => r.Headers["User-Agent"]).Returns(userAgent);

            // Act
            var result = request.Object.IsAndroid();

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void UserAgent_ValidRequest_ReturnsUserAgentHeader()
        {
            // Arrange
            var userAgent = "Mozilla/5.0 (Linux; Android 10; SM-G975F) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/89.0.4389.105 Mobile Safari/537.36";
            var request = new Mock<HttpRequest>();
            request.Setup(r => r.Headers["User-Agent"]).Returns(userAgent);

            // Act
            var result = request.Object.UserAgent();

            // Assert
            Assert.AreEqual(userAgent, result);
        }

        [TestMethod]
        public void ToHtmlDateTime_ValidDateTime_ReturnsFormattedString()
        {
            // Arrange
            var dateTime = new DateTime(2023, 6, 26, 10, 0, 0);
            var expectedString = "06/26/2023 10:00:00";

            // Act
            var result = dateTime.ToHtmlDateTime();

            // Assert
            Assert.AreEqual(expectedString, result);
        }
    }
}