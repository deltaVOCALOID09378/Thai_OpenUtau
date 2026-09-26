using OpenUtau.Api;
using Xunit;

namespace OpenUtau.Core.G2p {
    public class G2pPackTokenTest {
        [Theory]
        [InlineData(0, false)]
        [InlineData(1, false)]
        [InlineData(2, false)]
        [InlineData(3, false)]
        [InlineData(4, true)]
        [InlineData(9, true)]
        [InlineData(10, false)]
        public void AcceptsOnlyRealPhonemeTokens(int token, bool expected) {
            Assert.Equal(expected, G2pPack.IsPhonemeToken(token, 10));
        }
    }
}
