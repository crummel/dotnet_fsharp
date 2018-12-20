// #NoMono #NoMT #CodeGen #StringEncoding
// Regression test for DEV11:5317
// This is a very dynamic test.
// - Feed this script to FSI -> this will generate a C# file (oracle.cs)
// - Compile and run the C# file -> this will generate an F# file (testcase.fs)
// - The testcase will contain the expected values (coming from the C# oracle) and 
//   will match them against the the actual values calculated at runtime (F#)
//
// What we are doing is is really making sure that strings in F# assemblies are 
// encoded according to the specifications (which are, essentially, "do what C#
// does even if the C# is not fully ECMA compliant")
// 
// Using the Normalize(...) method is just an indirect way to test this. The direct verification
// would have been something like:
// - compile F# code with strings (UNICODE 0x0000 -> 0xffff)
// - open assembly with binary editor
// - look at the encoding
// - make sure the trailing byte is set to 0/1 accordingly.
// 
// Note: to keep the execution time within a reasonable limit, we only consider the range 0x0000 - 0x0123
//       (if you look at the code you'll see that nothing really interesting happens after 0xff... so the
//       odds of screwing up there are really small and unlikely to happen)
//
// To repro manually:
// del *.exe
// del oracle.cs
// del testcase.cs
// fsi --exec a.fsx > oracle.cs
// csc oracle.cs
// oracle.exe > testcase.fs
// fsc testcase.fs
// testcase.exe
//
// If a difference is detected, a message is printed and ERRORLEVEL is set to 1
//

// 
// Generate C# (oracle.cs)
open System
open System.IO

let outfile =
    fsi.CommandLineArgs
    |> Array.skipWhile((<>) "--")
    |> Array.skip 1
    |> Array.head
let content =
    [
        sprintf "using System.Collections.Generic;"
        sprintf "public class CSharpLibrary"
        sprintf "{"
        sprintf "    static public IEnumerable<int> input()"
        sprintf "    {"
    ] @
    [
        for a in [0 .. 0x123] do
            yield sprintf "        yield return (int)(\"\\u%04x\".Normalize(System.Text.NormalizationForm.FormKD)[0]);" a
    ] @
    [
        sprintf "    }"
        sprintf "    static void Main(string[] args)"
        sprintf "    {"
        sprintf "       int i = 0;"
        sprintf "       var sb = new System.Text.StringBuilder();"
        sprintf "       sb.AppendLine(\"module N.M\");"
        sprintf "       foreach(var v in input())"
        sprintf "       {"
        sprintf "           sb.AppendLine(System.String.Format(\"if {0} <> int (\\\"\\\\u{1:X4}\\\".Normalize(System.Text.NormalizationForm.FormKD).[0]) then printfn \\\"Difference detected at u{1:X4}\\\"; exit 1\", v, i++));"
        sprintf "       }"
        sprintf "       sb.AppendLine(\"exit 0 // if we get here, all is good!\");"
        sprintf "       System.IO.File.WriteAllText(args[0], sb.ToString());"
        sprintf "    }"
        sprintf "}"
    ]

File.WriteAllText(outfile, String.Join("\n", content))
