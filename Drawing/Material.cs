using System;
using System.Collections.Generic;
using System.Xml;
using NeroOS.Resources;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace NeroOS.Drawing
{
    class Material : Resource
    {
        Shader shader;

        const int textureCounts = 8;

        Texture2D[] textures = new Texture2D[textureCounts];
        string[] textureFileNames = new string[textureCounts];

        bool Transparent;
        bool Refract;
        bool Reflect;
        bool Transmissive;

        float kReflect;
        float kRefract;
        float kIOR;
        float kTrans;
        Vector3 kAmbient;
        Vector3 kDiffuse;
        Vector3 kSpecular;
        float kSpecularPower;
        float kRimCoeff;

        public override void OnLoad(XmlNode node)
        {
            foreach (XmlAttribute attrib in node.Attributes)
            {
                string[] attribs = attrib.Name.ToLower().Split('[');
                if (attribs.Length > 1)
                    attribs[1] = attribs[1].Replace("]", string.Empty);
                switch (attribs[0])
                {
                    case "reflect":
                        Reflect = bool.Parse(attrib.Value);
                        break;
                    case "refract":
                        Refract = bool.Parse(attrib.Value);
                        break;
                    case "transmissive":
                        Transmissive = bool.Parse(attrib.Value);
                        break;
                    case "transparent":
                        Transparent = bool.Parse(attrib.Value);
                        break;

                    case "kreflect":
                        kReflect = float.Parse(attrib.Value);
                        break;
                    case "krefract":
                        kRefract = float.Parse(attrib.Value);
                        break;
                    case "kior":
                        kIOR = float.Parse(attrib.Value);
                        break;
                    case "ktrans":
                        kTrans = float.Parse(attrib.Value);
                        break;
                        /*
                    case "kambient":
                        kAmbient = Utils.ParseVector3(attrib.Value);
                        break;
                    case "kdiffuse":
                        kDiffuse = Utils.ParseVector3(attrib.Value);
                        break;
                    case "kspecular":
                        kSpecular = Utils.ParseVector3(attrib.Value);
                        break;
                    case "kspecpower":
                        kSpecularPower = float.Parse(attrib.Value);
                        break;
                    case "krimcoeff":
                        kRimCoeff = float.Parse(attrib.Value);
                        break;
                        */
                    case "texture":
                        int index = int.Parse(attribs[1]);
                        if (index < textureCounts && index >= 0)
                            textureFileNames[index] = attrib.Value; 
                        break;
                   
                    case "name":
                        name = attrib.Value;
                        break;
                }
            }
        }

        public void SetupTextures(Canvas canvas)
        {
            for (int i = 0; i < textures.Length; i++)
            {
                if (textures[i] == null && textureFileNames[i] != string.Empty)
                    textures[i] = Texture2D.FromFile(canvas.GetDevice(), textureFileNames[i]);

                if (textures[i] != null)
                    canvas.GetDevice().Textures[i] = textures[i];
            }
        }

        public void SetupMaterial(Canvas canvas)
        {
            shader.SetupShader(canvas);
            
            SetupTextures(canvas);
        }
    }
}
