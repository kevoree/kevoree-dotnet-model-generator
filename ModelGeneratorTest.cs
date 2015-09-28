using System;
using NUnit.Framework;

namespace Org.Kevoree.ModelGenerator.Test
{

	[TestFixture]
	public class ModelGeneratorTest
	{
		public ModelGeneratorTest ()
		{
		}

		[Test]
		public void TestComponent()
		{
            //var res = new ModelGenerator().Analyze("org-kevoree-yield-version", "7.0.0");
			//Console.Write (res.ToString ());
		}

		[Test]
		public void NodeComponent()
		{
			// TODO : développer le composant de type Node !!!
		}

		[Test]
		public void ChannelComponent()
		{
			//new ModelGenerator ().Analyze (@"/home/mleduc/dev/dotnet/kevoree-dotnet-channel-local/bin/Release/LocalChannel.dll");
		}

		[Test]
		public void GroupComponent()
		{
			//new ModelGenerator ().Analyze (@"/home/mleduc/dev/dotnet/kevoree-dotnet-group-remotews/bin/Debug/kevoree-dotnet-group-remotews.dll");
		}
	}
}

