namespace MRB.DistributedLock.Test
{
    [TestClass]
    public class DistributedLockTest
    {
        [TestMethod]
        public void LockIsNotExpiredBeforeExpiresTime()
        {
            LockToken lockItem = new LockToken(this.GetType(), Guid.NewGuid()) { Expires = DateTime.UtcNow.AddMinutes(30) };
            Assert.IsFalse(lockItem.IsExpired());
        }

        [TestMethod]
        public void LockIsExpiredAfterExpiresTime()
        {
            LockToken lockItem = new LockToken(this.GetType(), Guid.NewGuid()) { Expires = DateTime.UtcNow.AddMinutes(-30) };
            Assert.IsTrue(lockItem.IsExpired());
        }

        [TestMethod]
        public void LockIsAcquiredWhenNoOneHasTheLock()
        {
            Mock<ILockRepostory> repoMock = new();
            Mock<ILogger<LockClient>> loggerMock = new();
            LockToken lockItem = new LockToken(this.GetType(), Guid.NewGuid());

            repoMock.Setup(s => s.TryGetTokenAsync(It.IsAny<Type>(), It.IsAny<Guid>()))
                .ReturnsAsync((ILockToken?)null);
            repoMock.Setup(s => s.TryCreateTokenAsync(It.IsAny<ILockToken>()))
                .ReturnsAsync(true);

            LockClient client = new LockClient(repoMock.Object, loggerMock.Object);
            using (var newLock = client.TryAcquireLockAsync(this.GetType(), Guid.NewGuid(), CancellationToken.None).Result)
            {
                Assert.IsNotNull(newLock);
            }
        }

        [TestMethod]
        public void ExistingLockBlocksNewLock()
        {
            Mock<ILockRepostory> repoMock = new();
            Mock<ILogger<LockClient>> loggerMock = new();
            LockToken lockItem = new LockToken(this.GetType(), Guid.NewGuid());

            Mock<ILockToken> existingToken = new();
            existingToken.Setup(s => s.IsExpired()).Returns(false);

            repoMock.Setup(s => s.TryGetTokenAsync(It.IsAny<Type>(), It.IsAny<Guid>()))
                .ReturnsAsync(existingToken.Object);

            LockClient client = new LockClient(repoMock.Object, loggerMock.Object);
            using (var newLock = client.TryAcquireLockAsync(this.GetType(), Guid.NewGuid(), CancellationToken.None).Result)
            {
                Assert.IsNull(newLock);
            }
        }


        [TestMethod]
        public void ExpiredLockDoesNotBlock()
        {
            Mock<ILockRepostory> repoMock = new();
            Mock<ILogger<LockClient>> loggerMock = new();
            LockToken lockItem = new LockToken(this.GetType(), Guid.NewGuid());

            Mock<ILockToken> existingToken = new();
            existingToken.Setup(s => s.IsExpired()).Returns(true);

            repoMock.Setup(s => s.TryGetTokenAsync(It.IsAny<Type>(), It.IsAny<Guid>()))
                .ReturnsAsync(existingToken.Object);
            repoMock.Setup(s => s.TryCreateTokenAsync(It.IsAny<ILockToken>()))
                .ReturnsAsync(true);
            repoMock.Setup(s => s.TryDeleteTokenAsync(It.IsAny<ILockToken>()))
                .ReturnsAsync(true);

            LockClient client = new LockClient(repoMock.Object, loggerMock.Object);
            var newLock = client.TryAcquireLockAsync(this.GetType(), Guid.NewGuid(), CancellationToken.None).Result;
            Assert.IsNotNull(newLock);
        }

        [TestMethod]
        public void ExpiredLockDeleteFailureBlocks()
        {
            Mock<ILockRepostory> repoMock = new();
            Mock<ILogger<LockClient>> loggerMock = new();
            LockToken lockItem = new LockToken(this.GetType(), Guid.NewGuid());

            Mock<ILockToken> existingToken = new();
            existingToken.Setup(s => s.IsExpired()).Returns(true); //token is expired

            repoMock.Setup(s => s.TryGetTokenAsync(It.IsAny<Type>(), It.IsAny<Guid>()))
                .ReturnsAsync(existingToken.Object);
            repoMock.Setup(s => s.TryCreateTokenAsync(It.IsAny<ILockToken>()))
                .ReturnsAsync(true);
            repoMock.Setup(s => s.TryDeleteTokenAsync(It.IsAny<ILockToken>()))
                .ReturnsAsync(false); //failed to delete

            LockClient client = new LockClient(repoMock.Object, loggerMock.Object);
            using (var newLock = client.TryAcquireLockAsync(this.GetType(), Guid.NewGuid(), CancellationToken.None).Result)
            {
                Assert.IsNull(newLock);
            }
        }
    }
}