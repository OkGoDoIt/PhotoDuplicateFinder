using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoHasher
{
	class Program
	{
		static void Main(string[] args)
		{
			var dups = args.SelectMany(arg => Directory.EnumerateFiles(arg, "*.jpg", SearchOption.AllDirectories))
				.Take(20000)
				.Select(f => new Photo(f, 9))
				.Where(p=>p.IsValidPhoto)
				.GroupBy(p => p.GetHashCode())
				.Where(g => g.Count() > 1);

			foreach (var dup in dups)
			{
				Console.WriteLine();
				Console.WriteLine($"{dup.Count()} duplicates of {dup.OrderByDescending(p=>p.Size).First().Path}:");
				Console.WriteLine(string.Join(Environment.NewLine, dup.Select(p=>p.Path)));
			}
			
		}
	}
}
