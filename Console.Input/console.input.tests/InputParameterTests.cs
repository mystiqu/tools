using NUnit.Framework;
using console.input;
using console.input.domain;

namespace console.input.tests
{
    [TestFixture]
    public class InputParameterTests
    {
        [OneTimeSetUp]
        public void Setup()
        {

        }

        [OneTimeTearDown]
        public void TearDown()
        {

        }

        [Test]
        public void Get_Help_Text_Complete()
        {
            InputSchema inputSchema = new InputSchema();
            inputSchema.PropertyPrefix = '-';
            inputSchema.Description = "this is a schema";
            inputSchema.Properties.Add(new InputProperty() { Key = "f", Type = PROPERTY_TYPE.KEY_VALUE, HelpText = "File path"});
            inputSchema.Properties.Add(new InputProperty() { Key = "p", Type = PROPERTY_TYPE.KEY_VALUE, HelpText = "Port number, 1-65536" });
            inputSchema.Properties.Add(new InputProperty() { Key = "noval", Type = PROPERTY_TYPE.KEY_ONLY, HelpText = "Some value"});
            inputSchema.Properties.Add(new InputProperty() { Key = "anothernoval", Type = PROPERTY_TYPE.KEY_ONLY, HelpText = "Yet another value" });

            InputParameterParser parser = new InputParameterParser(inputSchema, "-f", "file", "-p", "123", "-noval");
            string t = parser.GetHelpText();
            Assert.IsTrue(t.Contains("-f"));
            Assert.IsTrue(t.Contains("-p"));
            Assert.IsTrue(t.Contains("-noval"));
        }

        [Test]
        public void Get_Help_Text_Property()
        {
            InputSchema inputSchema = new InputSchema();
            inputSchema.PropertyPrefix = '-';
            inputSchema.Description = "this is a schema";
            inputSchema.Properties.Add(new InputProperty() { Key = "f", Type = PROPERTY_TYPE.KEY_VALUE, HelpText = "File path" });
            inputSchema.Properties.Add(new InputProperty() { Key = "p", Type = PROPERTY_TYPE.KEY_VALUE, HelpText = "Port number, 1-65536" });
            inputSchema.Properties.Add(new InputProperty() { Key = "noval", Type = PROPERTY_TYPE.KEY_ONLY, HelpText = "Some value" });
            inputSchema.Properties.Add(new InputProperty() { Key = "anothernoval", Type = PROPERTY_TYPE.KEY_ONLY, HelpText = "Yet another value" });

            InputParameterParser parser = new InputParameterParser(inputSchema, "-f", "file", "-p", "Port number", "-noval");
            string t = parser.GetHelpText("-p");
            Assert.IsTrue(t.Contains("Port number, 1-65536"));
        }

        [Test]
        public void Parse_Standard_Input_Parameter_String()
        {
            InputSchema inputSchema = new InputSchema();
            inputSchema.PropertyPrefix = '-';
            inputSchema.Description = "this is a schema";
            inputSchema.Properties.Add(new InputProperty() { Key = "f", Type = PROPERTY_TYPE.KEY_VALUE });
            inputSchema.Properties.Add(new InputProperty() { Key = "p", Type = PROPERTY_TYPE.KEY_VALUE });
            inputSchema.Properties.Add(new InputProperty() { Key = "noval", Type = PROPERTY_TYPE.KEY_ONLY });
            inputSchema.Properties.Add(new InputProperty() { Key = "anothernoval", Type = PROPERTY_TYPE.KEY_ONLY });

            InputParameterParser parser = new InputParameterParser(inputSchema, "-f", "file", "-p", "123", "-noval");
            parser.Parse();

            parser = new InputParameterParser(inputSchema, "-f", "file", "-anothernoval", "-p", "123", "-noval");
            parser.Parse();
            Assert.IsTrue(parser.Parameters.Count == 4);
            Assert.AreEqual(parser.Parameters[0].Key, "f");
            Assert.AreEqual(parser.Parameters[0].Value, "file");
            Assert.AreEqual(parser.Parameters[1].Key, "anothernoval");
            Assert.IsEmpty(parser.Parameters[1].Value);
            Assert.AreEqual(parser.Parameters[2].Key, "p");
            Assert.AreEqual(parser.Parameters[2].Value, "123");
            Assert.AreEqual(parser.Parameters[3].Key, "noval");
            Assert.IsEmpty(parser.Parameters[3].Value);
        }

        [Test]
        public void Parse_Standard_Input_Parameter_String_Using_Schema()
        {
            InputSchema inputSchema = new InputSchema();
            inputSchema.PropertyPrefix = '-';
            inputSchema.Description = "this is a schema";
            inputSchema.Properties.Add(new InputProperty() { Key = "f", Type = PROPERTY_TYPE.KEY_VALUE });
            inputSchema.Properties.Add(new InputProperty() { Key = "p", Type = PROPERTY_TYPE.KEY_VALUE });
            inputSchema.Properties.Add(new InputProperty() { Key = "noval", Type = PROPERTY_TYPE.KEY_ONLY });
            inputSchema.Properties.Add(new InputProperty() { Key = "anothernoval", Type = PROPERTY_TYPE.KEY_ONLY });

            InputParameterParser parser = new InputParameterParser(inputSchema, "-f", "file", "-anothernoval", "-p", "123", "-noval");
            parser.Parse();

            Assert.IsTrue(parser.Parameters.Count == 4);
            Assert.AreEqual(parser.Parameters[0].Key, "f");
            Assert.AreEqual(parser.Parameters[0].Value, "file");
            Assert.AreEqual(parser.Parameters[1].Key, "anothernoval");
            Assert.IsEmpty(parser.Parameters[1].Value);
            Assert.AreEqual(parser.Parameters[2].Key, "p");
            Assert.AreEqual(parser.Parameters[2].Value, "123");
            Assert.AreEqual(parser.Parameters[3].Key, "noval");
            Assert.IsEmpty(parser.Parameters[3].Value);
        }

        [Test]
        public void Parse_Standard_Input_Parameter_String_Using_Schema_Missing_Required_Key()
        {
            InputSchema inputSchema = new InputSchema();
            inputSchema.PropertyPrefix = '-';
            inputSchema.Description = "this is a schema";
            inputSchema.Properties.Add(new InputProperty() { Key = "f", Type = PROPERTY_TYPE.KEY_VALUE, Required = true });
            inputSchema.Properties.Add(new InputProperty() { Key = "p", Type = PROPERTY_TYPE.KEY_VALUE });
            inputSchema.Properties.Add(new InputProperty() { Key = "noval", Type = PROPERTY_TYPE.KEY_ONLY });
            inputSchema.Properties.Add(new InputProperty() { Key = "anothernoval", Type = PROPERTY_TYPE.KEY_ONLY });

            InputParameterParser parser = new InputParameterParser(inputSchema, "-anothernoval", "-p", "123", "-noval");
            parser.Parse();

            Assert.IsTrue(!parser.IsValid);
            Assert.AreEqual("Key 'f' is required but not present", parser.ValidatonError);
        }

        [Test]
        public void Parse_Standard_Input_Parameter_String_Using_Schema_Missing_Required_Value()
        {
            InputSchema inputSchema = new InputSchema();
            inputSchema.PropertyPrefix = '-';
            inputSchema.Description = "this is a schema";
            inputSchema.Properties.Add(new InputProperty() { Key = "f", Type = PROPERTY_TYPE.KEY_VALUE, Required = true });
            inputSchema.Properties.Add(new InputProperty() { Key = "p", Type = PROPERTY_TYPE.KEY_VALUE });
            inputSchema.Properties.Add(new InputProperty() { Key = "noval", Type = PROPERTY_TYPE.KEY_ONLY });
            inputSchema.Properties.Add(new InputProperty() { Key = "anothernoval", Type = PROPERTY_TYPE.KEY_ONLY });

            InputParameterParser parser = new InputParameterParser(inputSchema, "-f", "-anothernoval", "-p", "123", "-noval");
            parser.Parse();

            Assert.IsTrue(!parser.IsValid);
            Assert.AreEqual("Key 'f' is expected to have a value", parser.ValidatonError);
        }

        [Test]
        public void Parse_Standard_Input_Parameter_String_Using_Schema_Missing_Value_According_To_Type()
        {
            InputSchema inputSchema = new InputSchema();
            inputSchema.PropertyPrefix = '-';
            inputSchema.Description = "this is a schema";
            inputSchema.Properties.Add(new InputProperty() { Key = "f", Type = PROPERTY_TYPE.KEY_VALUE, Required = true });
            inputSchema.Properties.Add(new InputProperty() { Key = "p", Type = PROPERTY_TYPE.KEY_VALUE });
            inputSchema.Properties.Add(new InputProperty() { Key = "noval", Type = PROPERTY_TYPE.KEY_ONLY });
            inputSchema.Properties.Add(new InputProperty() { Key = "anothernoval", Type = PROPERTY_TYPE.KEY_ONLY });

            InputParameterParser parser = new InputParameterParser(inputSchema, "-f", "file", "-anothernoval", "-p", "-noval");
            parser.Parse();

            Assert.IsTrue(!parser.IsValid);
            Assert.AreEqual("Key 'p' is expected to have a value", parser.ValidatonError);
        }

        [Test]
        public void Parse_Standard_Input_Parameter_String_Using_Schema_Missing_Value_According_To_Schema()
        {
            InputSchema inputSchema = new InputSchema();
            inputSchema.PropertyPrefix = '-';
            inputSchema.Description = "this is a schema";
            inputSchema.Properties.Add(new InputProperty() { Key = "u", Type = PROPERTY_TYPE.UNKOWN, Required = true });
            inputSchema.Properties.Add(new InputProperty() { Key = "f", Type = PROPERTY_TYPE.KEY_VALUE, Required = true });
            inputSchema.Properties.Add(new InputProperty() { Key = "p", Type = PROPERTY_TYPE.KEY_VALUE });
            inputSchema.Properties.Add(new InputProperty() { Key = "noval", Type = PROPERTY_TYPE.KEY_ONLY });
            inputSchema.Properties.Add(new InputProperty() { Key = "anothernoval", Type = PROPERTY_TYPE.KEY_ONLY });

            InputParameterParser parser = new InputParameterParser(inputSchema, "-f", "file", "-u", "-anothernoval", "-p", "-noval");
            parser.Parse();

            Assert.IsTrue(!parser.IsValid);
            Assert.AreEqual("Key 'u' is required to have a value", parser.ValidatonError);
        }
    }
}