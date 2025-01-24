using HareIsle.Entities;
using HareIsle.Test.Assets;
using System.ComponentModel.DataAnnotations;

namespace HareIsle.Test
{
    [TestClass]
    public class ValidationTests
    {
        [TestMethod]
        public void ImpValidation_SuccessTest()
        {
            new Message<ImpRestrictObject>
            {
                Type = typeof(ImpRestrictObject).AssemblyQualifiedName,
                Payload = new ImpRestrictObject
                {
                    Value = 1
                }
            }.ThrowIfInvalid();
        }

        [TestMethod]
        public void ImpValidation_FailTest()
        {
            Assert.ThrowsException<ValidationException>(() => new Message<ImpRestrictObject>
            {
                Type = typeof(ImpRestrictObject).AssemblyQualifiedName,
                Payload = new ImpRestrictObject
                {
                    Value = -1
                }
            }.ThrowIfInvalid());
        }

        [TestMethod]
        public void AttrValidation_SuccessTest()
        {
            new Message<AttrRestrictObject>
            {
                Type = typeof(AttrRestrictObject).AssemblyQualifiedName,
                Payload = new AttrRestrictObject
                {
                    FirstNumber = 1,
                    SecondNumber = 2
                }
            }.ThrowIfInvalid();
        }

        [TestMethod]
        public void AttrValidation_FailTest()
        {
            Assert.ThrowsException<ValidationException>(() => new Message<AttrRestrictObject>
            {
                Type = typeof(AttrRestrictObject).AssemblyQualifiedName,
                Payload = new AttrRestrictObject
                {
                    FirstNumber = 100,
                    SecondNumber = 2
                }
            }.ThrowIfInvalid());
        }

        [TestMethod]
        public void TypeRequired_Test()
        {
            Assert.ThrowsException<ValidationException>(() => new Message<AttrRestrictObject>
            {
                Payload = new AttrRestrictObject()
            }.ThrowIfInvalid());
        }

        [TestMethod]
        public void PayloadRequired_Test()
        {
            Assert.ThrowsException<ValidationException>(() => new Message<AttrRestrictObject>
            {
                Type = typeof(AttrRestrictObject).AssemblyQualifiedName
            }.ThrowIfInvalid());
        }

        [TestMethod]
        public void TypeMismatch_Test()
        {
            Assert.ThrowsException<ValidationException>(() => new Message<AttrRestrictObject>
            {
                Type = typeof(ImpRestrictObject).AssemblyQualifiedName,
                Payload = new AttrRestrictObject()
            }.ThrowIfInvalid());
        }
    }
}