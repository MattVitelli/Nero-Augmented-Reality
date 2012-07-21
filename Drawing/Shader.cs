using System;
using System.Xml;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NeroOS.Resources;
using NeroOS.Core;

namespace NeroOS.Drawing
{
    public class Shader : Resource
    {
        string psFileName = "";
        string vsFileName = "";

        PixelShader ps;
        VertexShader vs;
        bool compiled = false;

        public int VSTarget = 2;
        public int PSTarget = 3;
        string errorMessage = null;

        public override void OnDestroy()
        {
            if (ps != null)
                ps.Dispose();
            ps = null;
            if (vs != null)
                vs.Dispose();
            vs = null;
        }

        public override void OnLoad(XmlNode node)
        {
            foreach (XmlAttribute attrib in node.Attributes)
            {
                switch (attrib.Name.ToLower())
                {
                    case "psfilename":
                        psFileName = attrib.Value;
                        break;
                    case "vsfilename":
                        vsFileName = attrib.Value;
                        break;
                    case "pstarget":
                        PSTarget = int.Parse(attrib.Value);
                        break;
                    case "vstarget":
                        VSTarget = int.Parse(attrib.Value);
                        break;
                    case "name":
                        name = attrib.Value;
                        break;
                }
            }
        }

        public void CompileFromFiles(Canvas canvas, string psName, string vsName)
        {
            this.psFileName = psName;
            this.vsFileName = vsName;
            ShaderProfile psProf = ShaderProfile.PS_1_1;
            switch (PSTarget)
            {
                case 2:
                    psProf = ShaderProfile.PS_2_0;
                    break;
                case 3:
                    psProf = ShaderProfile.PS_3_0;
                    break;
            }
            ShaderProfile vsProf = ShaderProfile.VS_1_1;
            switch (VSTarget)
            {
                case 2:
                    vsProf = ShaderProfile.VS_2_0;
                    break;
                case 3:
                    vsProf = ShaderProfile.VS_3_0;
                    break;
            }
            CompiledShader psShader = ShaderCompiler.CompileFromFile(psFileName, null, null, CompilerOptions.PackMatrixRowMajor, "main", psProf, TargetPlatform.Windows);
            Log.GetInstance().WriteLine(psShader.ErrorsAndWarnings);
            CompiledShader vsShader = ShaderCompiler.CompileFromFile(vsFileName, null, null, CompilerOptions.PackMatrixRowMajor, "main", vsProf, TargetPlatform.Windows);
            Log.GetInstance().WriteLine(vsShader.ErrorsAndWarnings);
            errorMessage = null;
            if (vsShader.ErrorsAndWarnings.Length > 1)
                errorMessage = "Vertex Shader: " + vsShader.ErrorsAndWarnings;
            if (psShader.ErrorsAndWarnings.Length > 1)
            {
                if (errorMessage == null)
                    errorMessage = "Pixel Shader: " + psShader.ErrorsAndWarnings;
                else
                    errorMessage = errorMessage + "\n Pixel Shader: " + psShader.ErrorsAndWarnings;
            }
            if (psShader.Success && vsShader.Success)
            {
                ps = new PixelShader(canvas.GetDevice(), psShader.GetShaderCode());
                vs = new VertexShader(canvas.GetDevice(), vsShader.GetShaderCode());
                compiled = true;
            }
        }

        public void SetupShader(Canvas canvas)
        {
            if (!compiled)
            {
                CompileFromFiles(canvas, psFileName, vsFileName);
                if (errorMessage != null)
                    Log.GetInstance().WriteLine(errorMessage);
                return;
            }
            canvas.GetDevice().PixelShader = ps;
            canvas.GetDevice().VertexShader = vs;
        }
    }
}
