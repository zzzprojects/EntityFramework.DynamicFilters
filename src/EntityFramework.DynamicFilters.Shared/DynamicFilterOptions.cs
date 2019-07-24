using System;

namespace EntityFramework.DynamicFilters
{
    public class DynamicFilterOptions
    {
        /// <summary>
        /// If not null, this delegate should return true if the filter should be applied to the given entity Type.
        /// False if not.  Allows additional logic to be applied to determine if the filter should be applied to an Entity of the type.
        /// i.e. To apply the filter to all entities of a particular interface but not if those entities also implement another interface.
        /// </summary>
        public Func<Type, bool> SelectEntityTypeCondition { get; internal set; } = null;

        /// <summary>
        /// If false, the filter will only be applied to the main entity of the query.
        /// Default = true.
        /// </summary>
        public bool ApplyToChildProperties { get; internal set; } = true;

        /// <summary>
        /// If false, the filter will not be applied if it was already applied to a parent entity.
        /// Default = true
        /// </summary>
        public bool ApplyRecursively { get; internal set; } = true;
    }

    public class DynamicFilterConfig
    {
        private DynamicFilterOptions _Options = new DynamicFilterOptions();

        /// <summary>
        /// If not null, this delegate should return true if the filter should be applied to the given entity Type.
        /// False if not.  Allows additional logic to be applied to determine if the filter should be applied to an Entity of the type.
        /// i.e. To apply the filter to all entities of a particular interface but not if those entities also implement another interface.
        /// </summary>
        /// <param name="selectForEntityType"></param>
        /// <returns></returns>
        public DynamicFilterOptions SelectEntityTypeCondition(Func<Type, bool> selectEntityTypeCondition = null)
        {
            _Options.SelectEntityTypeCondition = selectEntityTypeCondition;
            return _Options;
        }

        /// <summary>
        /// If false, the filter will only be applied to the main entity of the query.
        /// Default = true
        /// </summary>
        /// <param name="applyToChildProperties"></param>
        /// <returns></returns>
        public DynamicFilterOptions ApplyToChildProperties(bool applyToChildProperties = true)
        {
            _Options.ApplyToChildProperties = applyToChildProperties;
            return _Options;
        }

        /// <summary>
        /// If false, the filter will not be applied if it was already applied to a parent entity.
        /// Default = true
        /// </summary>
        /// <param name="applyRecursively"></param>
        /// <returns></returns>
        public DynamicFilterOptions ApplyRecursively(bool applyRecursively = true)
        {
            _Options.ApplyRecursively = applyRecursively;
            return _Options;
        }
    }
}
