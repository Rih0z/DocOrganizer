using System;

namespace DocOrganizer.Application.Attributes
{
    /// <summary>
    /// 🏗️ V3.0.009 プロバイダー属性 - 自動発見・登録用メタデータ
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ImageProcessingProviderAttribute : Attribute
    {
        public string Name { get; }
        public int Priority { get; set; } = 50;

        public ImageProcessingProviderAttribute(string name)
        {
            Name = name;
        }
    }
}