using NUnit.Framework;

namespace FTrees.Tests {
    
    [TestFixture]
    public class ImmutableOrderedSetTests
    {
        [Test]
        public void Construct_WithMultipleValues_ShouldBeSortedAndUnique()
        {
            var set = ImmutableOrderedSet.Create(3, 1, 2, 2, 3, 4, 5, 6, 7, 8, 9, 10);
            Assert.AreEqual(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }, set);
        }

        [Test]
        public void Add_NewValue_ShouldBeAddedInOrder()
        {
            var set = ImmutableOrderedSet.Create(1, 3, 4, 5, 6, 7, 8, 9, 10);
            var updatedSet = set.Add(2);
            Assert.AreEqual(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }, updatedSet);
        }

        [Test]
        public void Add_ExistingValue_ShouldNotChangeSet()
        {
            var set = ImmutableOrderedSet.Create(1, 2, 3, 4, 5, 6, 7, 8, 9);
            var updatedSet = set.Add(2);
            Assert.AreEqual(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }, updatedSet);
        }

        [Test]
        public void Remove_ExistingValue_ShouldRemoveValue()
        {
            var set = ImmutableOrderedSet.Create(1, 2, 3, 4, 5, 6, 7, 8, 9);
            var updatedSet = set.Remove(2);
            Assert.AreEqual(new[] { 1, 3, 4, 5, 6, 7, 8, 9 }, updatedSet);
        }

        [Test]
        public void Remove_NonExistingValue_ShouldNotChangeSet()
        {
            var set = ImmutableOrderedSet.Create(1, 2, 3, 4, 5, 6, 7, 8, 9);
            var updatedSet = set.Remove(10);
            Assert.AreEqual(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }, updatedSet);
        }

        [Test]
        public void MergeWith_OtherSet_ShouldContainAllUniqueValuesSorted()
        {
            var set1 = ImmutableOrderedSet.Create(1, 3, 4, 5, 6, 7, 8, 9);
            var set2 = ImmutableOrderedSet.Create(2, 3, 4, 8, 9, 10, 11, 12);
            var mergedSet = set1.MergeWith(set2);
            Assert.AreEqual(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 }, mergedSet);
        }
    }
}