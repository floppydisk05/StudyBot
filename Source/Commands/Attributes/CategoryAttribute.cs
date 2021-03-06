using System;

namespace StudyBot.Commands.Attributes
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Parameter, AllowMultiple = false)]
    public sealed class CategoryAttribute : Attribute
    {
        /// <summary>
        /// Gets the category of this command
        /// </summary>
        public Category Category { get; }

        public CategoryAttribute(Category category)
        {
            this.Category = category;
        }
    }

    public enum Category
    {
        Main, Misc, Staff, Owner
    }
}