using System;
using System.Collections.Generic;
using System.IO;

namespace NeroOS.Drawing
{
    public static class CanvasShaderConstants
    {
        public static int VC_MODELVIEW = 0;
        public static int VC_EYEPOS = 1;
        public static int VC_INVTEXRES = 5;
        public static int VC_WORLD = 4;

        public static int PC_EYEPOS = 4;
        public static int PC_FARPLANE = 5;

        static void WriteCommand(StreamWriter writer, string commandName, int index)
        {
            writer.Write("#define ");
            writer.Write(commandName);
            writer.Write(" C");
            writer.Write(index);
            writer.Write("\n");
        }

        public static void AuthorShaderConstantFile()
        {
            using (FileStream fs = new FileStream("Shaders/ShaderConst.h", FileMode.Create))
            {
                using (StreamWriter wr = new StreamWriter(fs))
                {
                    WriteCommand(wr, "VC_MODELVIEW", VC_MODELVIEW);
                    WriteCommand(wr, "VC_WORLD", VC_WORLD);
                    WriteCommand(wr, "VC_EYEPOS", VC_EYEPOS);
                    WriteCommand(wr, "VC_INVTEXRES", VC_INVTEXRES);
                    WriteCommand(wr, "PC_EYEPOS", PC_EYEPOS);
                    WriteCommand(wr, "PC_FARPLANE", PC_FARPLANE);
                }
            }

        }
    }
}
