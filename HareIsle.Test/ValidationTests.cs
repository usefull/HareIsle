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
        [ExpectedException(typeof(ValidationException))]
        public void ImpValidation_FailTest()
        {
            new Message<ImpRestrictObject>
            {
                Type = typeof(ImpRestrictObject).AssemblyQualifiedName,
                Payload = new ImpRestrictObject
                {
                    Value = -1
                }
            }.ThrowIfInvalid();
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
        [ExpectedException(typeof(ValidationException))]
        public void AttrValidation_FailTest()
        {
            new Message<AttrRestrictObject>
            {
                Type = typeof(AttrRestrictObject).AssemblyQualifiedName,
                Payload = new AttrRestrictObject
                {
                    FirstNumber = 100,
                    SecondNumber = 2
                }
            }.ThrowIfInvalid();
        }

        [TestMethod]
        [ExpectedException(typeof(ValidationException))]
        public void TypeRequired_Test()
        {
            new Message<AttrRestrictObject>
            {
                Payload = new AttrRestrictObject()
            }.ThrowIfInvalid();
        }

        [TestMethod]
        [ExpectedException(typeof(ValidationException))]
        public void PayloadRequired_Test()
        {
            new Message<AttrRestrictObject>
            {
                Type = typeof(AttrRestrictObject).AssemblyQualifiedName
            }.ThrowIfInvalid();
        }

        [TestMethod]
        [ExpectedException(typeof(ValidationException))]
        public void TypeMismatch_Test()
        {
            new Message<AttrRestrictObject>
            {
                Type = typeof(ImpRestrictObject).AssemblyQualifiedName,
                Payload = new AttrRestrictObject()
            }.ThrowIfInvalid();
        }
    }
}