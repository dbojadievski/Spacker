using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace ShaderPacker
{
    class Spacker
    {
        static Dictionary<String, Int32> UsedNames                          = new Dictionary<string, int> ( );
        static String [ ] ShaderExtensions                                  = new String [ ] { "glsl", "hlsl", "cg", "c" };
        static String [ ] OutputExtension                                   = new String [ ] { "h", "cs", "js" };
        static void Main ( string [ ] args )
        {
            var basePath                                                    = Environment.CurrentDirectory;

            if ( args.Length > 0 )
                basePath                                                    = args [ 0 ];

            var output                                                      = "shaders.h";
            if ( args.Length > 1 )
                output                                                      = args [ 1 ];

            var minify                                                      = args.Contains("-min");
            String type;
            Boolean isOutputValid                                           = ValidateOutput ( output, out type );
            if ( !isOutputValid )
            {
                Console.WriteLine ( "Output type must be a .h, .cs or .js file." );
                return;
            }
            else
            {
                var directory                                                   = new DirectoryInfo(basePath);
                String [ ] masks = new String [ ShaderExtensions.Count ( ) ];
                for ( var currIndex = 0; currIndex < masks.Count ( ); currIndex++ )
                    masks [ currIndex ]                                         = String.Format ( "*.{0}", ShaderExtensions [ currIndex ] );
                var shaders                                                     = masks.SelectMany ( directory.EnumerateFiles ).ToArray ( );
                if ( shaders.Length == 0 )
                {
                    Console.WriteLine ( "There are no shaders in the specified source folder." );
                    Console.WriteLine ( "Usage:" );
                    Console.WriteLine ( "spacker [base directory] [output file] [-min]" );
                    Console.WriteLine ( "base directory: directory containing shaders in *.glsl files. All shader files will be packed into the result file." );
                    Console.WriteLine ( "output file: path and name of the result file. If the file exists it will be overwritten." );
                    Console.WriteLine ( "-min : If you define -min switch result will be stripped of comments and blank characters." );
                    Console.WriteLine ( );
                    Console.WriteLine ( "Default values are:" );
                    Console.WriteLine ( "spacker .\\ shaders.h" );
                    Console.WriteLine ( );
                    Console.ReadKey ( );
                    return;
                }
                using ( var tw                                                  = File.CreateText ( Path.Combine ( basePath, output ) ) )
                {
                    GenerateBeginning ( tw, type );

                    //start object definition


                    //get content of every shader and create property containing code content of the shader
                    foreach ( var shader in shaders )
                    {
                        var shaderName                                          = Path.GetFileNameWithoutExtension(shader.Name);
                        if ( shaderName != null )
                        {
                            shaderName = shaderName.Replace ( '.', '_' );
                            shaderName = shaderName.Replace ( ' ', '_' );
                            shaderName = shaderName.Replace ( '-', '_' );

                            shaderName = GenerateShaderName ( shaderName );
                            GenerateShaderAssignment ( minify, tw, shaderName, type );
                        }

                        var lines                                               = File.ReadAllLines(shader.FullName);
                        for ( var i                                             = 0; i < lines.Length; i++ )
                        {
                            var line = lines[i];
                            if ( minify )
                            {
                                line                                            = Regex.Replace ( line, @"\s+", " " );
                                // Remove comments.
                                var exp                                         = new Regex("//.*");
                                var stripLine = exp.Replace(line, "").Trim();
                                if ( !string.IsNullOrEmpty ( stripLine ) )
                                 tw.Write ( " " + stripLine );
                            }
                            else
                                tw.Write ( "{0}", line );

                        }
                        tw.WriteLine ( "\";");
                    }
                    GenerateEnding ( tw, type );
                    tw.Close ( );
                }
            }
        }

        static Boolean ValidateOutput ( String output, out String type )
        {
            Boolean isValid                                                     = false;

            String  [ ] parts                                                   = output.Split ('.');
            Int32 numParts                                                      = parts.Count ( );
            if ( numParts >= 2 )
            {
                String extension                                                = parts [ numParts - 1 ];
                type                                                            = extension;
                if ( OutputExtension.Contains ( extension ) )
                    isValid                                                     = true;
            }
            else
                type                                                            = null;
            return isValid;
        }

        static String GenerateShaderName ( String shaderName )
        {
            if ( UsedNames.ContainsKey ( shaderName ) )
                shaderName                                                      = String.Format ( "{0}_{1}", shaderName, UsedNames [ shaderName ]++ );
            else
                UsedNames [ shaderName ]                                        = 0;
            shaderName                                                          = shaderName.Replace ( '.', '_' );
            shaderName                                                          = shaderName.Replace ( ' ', '_' );
            shaderName                                                          = shaderName.Replace ( '-', '_' );
            return shaderName;
        }
        static void GenerateBeginning ( StreamWriter tw, String type )
        {
            tw.WriteLine ( "/*" );
            tw.WriteLine ( "* This file has been generated by the Shader packer utility. Do not change this file manualy as your changes" );
            tw.WriteLine ( "* will get lost when the file is regenerated. Original content is located in *.c, *.cg, *.glsl and *.hlsl files." );
            tw.WriteLine ( "* Shader packer was authored by Dino Bojadjievski." );
            tw.WriteLine ( "*/" );

            switch ( type )
            {
                case "h":
                    break;
                case "js":
                    tw.WriteLine ( "if (!window.sPacker) window.spacker = {};");
                    break;
                case "cs":
                    tw.WriteLine ( "using System;" );
                    tw.WriteLine ( "namespace Shaders " );
                    tw.WriteLine ( "{" );
                    tw.WriteLine ( "\tpublic static class Spacker" );
                    tw.WriteLine ( "\t{" );
                    break;
                default:
                    System.Diagnostics.Debug.Assert ( false );
                    break;
            }
        }

        static void GenerateShaderAssignment ( Boolean minify, StreamWriter tw, String shaderName, String type )
        {
            switch ( type )
            {
                case "h":
                    tw.Write ( "const char * pStr_{0} = \"{1}", shaderName, minify ? "\"" : "" );
                    break;
                case "cs":
                    tw.Write ( "\t\tpublic static String {0} = \"{1}", shaderName, minify ? "\"" : "" );
                    break;
                case "js":
                    tw.Write ( "spacker.{0}=\"{1}", shaderName, minify ? "\"" : "" );
                    break;
                default:
                    System.Diagnostics.Debug.Assert ( false );
                    break;
            }
        }

        static void GenerateEnding ( StreamWriter tw, String type )
        {
            switch ( type )
            {
                case "h":
                case "js":
                    break;
                case "cs":
                    tw.WriteLine ( "\t}" );
                    tw.Write ( "}" );
                    break;
                default:
                    System.Diagnostics.Debug.Assert ( false );
                    break;
            }
        }
    }
}