using System;
using System.Xml;

namespace NeroOS.Resources
{
    public abstract class Resource
    {
        protected string name;
        public string Name { get { return name; } set { name = value; } }

        public virtual void OnDestroy() { }
        public virtual void OnLoad(XmlNode node) { }
    }
}
