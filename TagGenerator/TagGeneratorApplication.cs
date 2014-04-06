using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.External.Structure;
using Emgu.CV.Structure;

namespace TagGenerator
{
    class TagGeneratorApplication
    {
        static void Main(string[] args)
        {           
            try
            {
                using (StreamReader sr = new StreamReader("tagdefinition.txt"))
                {
                    String line;
                    do
                    {
                        line = sr.ReadLine();
                        if (line != null)
                        {
                            var tokens = line.Split(' ');
                            Console.WriteLine("Generating: " + tokens[0] + " " + tokens[1]);
                            GenerateAndSaveTag(tokens[0], tokens[1]);
                        }
                    } while (line != null);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error...");
                Console.WriteLine(e.Message);
            }
            //Console.Read();
        }

        private static void GenerateAndSaveTag(String id, String code)
        {
            var cells = 5.0; //5x5
            var size = 420.0;
            var output = new Image<Gray, Byte>((int)size, (int)size, new Gray(255));
            Gray color;
            int i = 0;

            for (int y = 0; y < 5; y++)
            {
                for (int x = 0; x < 5; x++)
                {
                    if (code[i++] == '1')
                    {
                        color = new Gray(255);
                    }
                    else
                    {
                        color = new Gray(0);
                    }
                    var r = new Rectangle();

                    output.Draw(new Rectangle(
                        (int) ((x + 1.0) * (size / (cells+2.0))),
                        (int) ((y + 1.0) * (size / (cells+2.0))),
                        (int) (size / (cells+2.0)), 
                        (int) (size / (cells+2.0))), color, -1);
                }
            }            
            output.Save(id+".png");
        }

    }
}
