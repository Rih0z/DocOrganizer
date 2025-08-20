using System;

namespace DocOrganizer.Application.Attributes
{
    /// <summary>
    /// 🏗️ プロバイダー自動発見属性
    /// 責務: DI自動登録用メタデータ提供
    /// 設計: Attribute-based Registration Pattern
    /// 参考: ASP.NET Core Service Registration, OSS Plugin Discovery
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ValidationProviderAttribute : Attribute
    {
        /// <summary>
        /// プロバイダー名
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 優先度（デフォルト: 50）
        /// HEIC: 100, GIF: 90, Standard: 80
        /// </summary>
        public int Priority { get; set; } = 50;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="name">プロバイダー名</param>
        public ValidationProviderAttribute(string name)
        {
            Name = name;
        }
    }
}