using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Dungeon.Tests.PlayMode
{
    public class SamplePlayModeTests
    {
        [Test]
        public void Sample_Passes()
        {
            Assert.Pass();
        }

        [UnityTest]
        public IEnumerator Sample_PassesAfterOneFrame()
        {
            yield return null;
            Assert.Pass();
        }
    }
}
