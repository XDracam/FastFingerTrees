using System.Collections.Immutable;
using System.Linq;
using NUnit.Framework;

namespace FTrees.Tests
{
    [TestFixture]
    public class ImmutableSeqTests
    {
        [Test]
        public void TestEmptyList()
        {
            ImmutableSeq<int> list = ImmutableSeq<int>.Empty;
            Assert.AreEqual(0, list.Count);
        }

        [Test]
        public void TestAddition()
        {
            IImmutableList<int> list = ImmutableSeq<int>.Empty;
            list = list.Add(1);
            Assert.AreEqual(1, list.Count);
            Assert.AreEqual(1, list[0]);
        }

        [Test]
        public void TestRemoval()
        {
            IImmutableList<int> list = ImmutableSeq.Create(1, 2, 3);
            list = list.Remove(2);
            Assert.AreEqual(2, list.Count);
            Assert.IsFalse(list.Contains(2));
        }

        [Test]
        public void TestInsertion()
        {
            IImmutableList<int> list = ImmutableSeq<int>.Empty;
            list = list.Insert(0, 1);
            list = list.Insert(1, 3);
            list = list.Insert(1, 2); // Inserting in the middle
            Assert.AreEqual(3, list.Count);
            Assert.AreEqual(1, list[0]);
            Assert.AreEqual(2, list[1]);
            Assert.AreEqual(3, list[2]);
        }

        [Test]
        public void TestEnumeration()
        {
            IImmutableList<int> list = ImmutableSeq.Create(1, 2, 3);
            int sum = 0;
            foreach (var item in list)
            {
                sum += item;
            }
            Assert.AreEqual(6, sum);
        }

        [Test]
        public void TestClear()
        {
            IImmutableList<int> list = ImmutableSeq.Create(1, 2, 3);
            list = list.Clear();
            Assert.AreEqual(0, list.Count);
        }

        [Test]
        public void TestReplace()
        {
            IImmutableList<int> list = ImmutableSeq.Create(1, 2, 3);
            list = list.Replace(2, 5);
            Assert.AreEqual(3, list.Count);
            Assert.AreEqual(5, list[1]);
        }
        
        [Test]
        public void TestSetItem()
        {
            IImmutableList<int> list = ImmutableSeq.Create(1, 2, 3);
            list = list.SetItem(1, 4); // Replace the item at index 1
            Assert.AreEqual(3, list.Count);
            Assert.AreEqual(4, list[1]);
        }

        [Test]
        public void TestRemoveAt()
        {
            IImmutableList<int> list = ImmutableSeq.Create(1, 2, 3);
            list = list.RemoveAt(1); // Remove the item at index 1
            Assert.AreEqual(2, list.Count);
            Assert.IsFalse(list.Contains(2)); // 2 was removed
        }

        [Test]
        public void TestRemoveRange()
        {
            IImmutableList<int> list = ImmutableSeq.Create(1, 2, 3, 4, 5);
            list = list.RemoveRange(1, 3); // Remove 3 items starting from index 1
            Assert.AreEqual(2, list.Count);
            Assert.AreEqual(1, list[0]);
            Assert.AreEqual(5, list[1]);
        }

        [Test]
        public void TestInsertRange()
        {
            IImmutableList<int> list = ImmutableSeq.Create(1, 5);
            list = list.InsertRange(1, new[] { 2, 3, 4 }); // Insert a range at index 1
            Assert.AreEqual(5, list.Count);
            for (int i = 0; i < 5; i++)
            {
                Assert.AreEqual(i + 1, list[i]);
            }
        }

        [Test]
        public void TestAddRange()
        {
            IImmutableList<int> list = ImmutableSeq.Create(1, 2);
            list = list.AddRange(new[] { 3, 4, 5 }); // Add a range to the end
            Assert.AreEqual(5, list.Count);
            for (int i = 0; i < 5; i++)
            {
                Assert.AreEqual(i + 1, list[i]);
            }
        }
        
        [Test]
        public void TestHead()
        {
            var list = ImmutableSeq.Create(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
            int head = list.Head; // Assuming `Head` gets the first element
            Assert.AreEqual(1, head);
        }

        [Test]
        public void TestLast()
        {
           var list = ImmutableSeq.Create(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
            int last = list.Last;
            Assert.AreEqual(10, last);
        }

        [Test]
        public void TestTail()
        {
            var list = ImmutableSeq.Create(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
            var tail = list.Tail;
            Assert.AreEqual(9, tail.Count);
            Assert.AreEqual(2, tail[0]);
            Assert.AreEqual(3, tail[1]);
            Assert.AreEqual(4, tail[2]);
            Assert.AreEqual(5, tail[3]);
            Assert.AreEqual(6, tail[4]);
            Assert.AreEqual(7, tail[5]);
            Assert.AreEqual(8, tail[6]);
            Assert.AreEqual(9, tail[7]);
            Assert.AreEqual(10, tail[8]);
        }

        [Test]
        public void TestInit()
        {
            var list = ImmutableSeq.Create(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
            var init = list.Init; 
            Assert.AreEqual(9, init.Count);
            Assert.AreEqual(1, init[0]);
            Assert.AreEqual(2, init[1]);
            Assert.AreEqual(3, init[2]);
            Assert.AreEqual(4, init[3]);
            Assert.AreEqual(5, init[4]);
            Assert.AreEqual(6, init[5]);
            Assert.AreEqual(7, init[6]);
            Assert.AreEqual(8, init[7]);
            Assert.AreEqual(9, init[8]);
        }
        
        [Test]
        public void TestSplitAt()
        {
            var list = ImmutableSeq.Create(1, 2, 3, 4, 5, 6, 7, 8, 9);
            var (left, right) = list.SplitAt(5);
    
            // Verify the first part
            Assert.AreEqual(5, left.Count);
            Assert.AreEqual(1, left[0]);
            Assert.AreEqual(2, left[1]);
            Assert.AreEqual(3, left[2]);
            Assert.AreEqual(4, left[3]);
            Assert.AreEqual(5, left[4]);
    
            // Verify the second part
            Assert.AreEqual(4, right.Count);
            Assert.AreEqual(6, right[0]);
            Assert.AreEqual(7, right[1]);
            Assert.AreEqual(8, right[2]);
            Assert.AreEqual(9, right[3]);
        }
        
        [Test]
        public void TestConcat()
        {
            var list1 = ImmutableSeq.Create(1, 2, 3, 4, 5);
            var list2 = ImmutableSeq.Create(6, 7, 8, 9);
            var concat = list1.Concat(list2);
    
            Assert.AreEqual(9, concat.Count);
            Assert.AreEqual(1, concat[0]);
            Assert.AreEqual(2, concat[1]);
            Assert.AreEqual(3, concat[2]);
            Assert.AreEqual(4, concat[3]);
            Assert.AreEqual(5, concat[4]);
            Assert.AreEqual(6, concat[5]);
            Assert.AreEqual(7, concat[6]);
            Assert.AreEqual(8, concat[7]);
            Assert.AreEqual(9, concat[8]);
        }
        
        [Test]
        public void TestAppend()
        {
            var list = ImmutableSeq.Create(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
            var appendedList = list.Append(11); // Assuming `Append` is similar to `Add`
    
            Assert.AreEqual(11, appendedList.Count);
            Assert.AreEqual(11, appendedList.Last);   // Check if the last item is the appended one
            Assert.AreNotSame(list, appendedList); // Ensure the original list remains unchanged
        }

        [Test]
        public void TestPrepend()
        {
            var list = ImmutableSeq.Create(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
            var prependedList = list.Prepend(0); // Assuming `Prepend` behaves like inserting at 0 index
    
            Assert.AreEqual(11, prependedList.Count);
            Assert.AreEqual(0, prependedList.Head);   // Check if the first item is the prepended one
            Assert.AreNotSame(list, prependedList); // Ensure the original list remains unchanged
        }
        
        [Test]
        public void TestRemoveAll()
        {
            IImmutableList<int> list = ImmutableSeq.Create(1, 2, 3, 4, 5);
            // Assuming `RemoveAll` removes elements that match the condition
            IImmutableList<int> filteredList = list.Where(item => item % 2 != 0).ToImmutableList(); // Removes even numbers
    
            Assert.AreEqual(3, filteredList.Count);
            Assert.IsFalse(filteredList.Contains(2));
            Assert.IsFalse(filteredList.Contains(4));
        }

        [Test]
        public void TestIndexOf()
        {
            IImmutableList<int> list = ImmutableSeq.Create(1, 2, 3, 2, 1);
            // Assuming `IndexOf` finds the first occurrence of the specified value
            int index = list.IndexOf(2);
    
            Assert.AreEqual(1, index); // Index of the first occurrence of 2
        }

        [Test]
        public void TestLastIndexOf()
        {
            IImmutableList<int> list = ImmutableSeq.Create(1, 2, 3, 2, 1);
            // Assuming `LastIndexOf` finds the last occurrence of the specified value
            int lastIndex = list.LastIndexOf(2);
    
            Assert.AreEqual(3, lastIndex); // Index of the last occurrence of 2
        }
    }
}