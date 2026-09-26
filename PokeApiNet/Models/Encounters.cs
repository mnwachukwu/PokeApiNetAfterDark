using System.Collections.Generic;

namespace PokeApiNetAfterDark.Models
{
    /// <summary>
    /// Methods by which the player might can encounter Pokémon
    /// in the wild, e.g., walking in tall grass.
    /// </summary>
    public class EncounterMethod : NamedApiResource
    {
        /// <summary>
        /// The identifier for this resource.
        /// </summary>
        public override int Id { get; set; }

        internal new static string ApiEndpoint { get; } = "encounter-method";

        /// <summary>
        /// The name for this resource.
        /// </summary>
        public override string Name { get; set; } = null!;

        /// <summary>
        /// A good value for sorting.
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// The name of this resource listed in different
        /// languages.
        /// </summary>
        public List<Names> Names { get; set; } = null!;
    }

    /// <summary>
    /// Conditions which affect what pokemon might appear in the
    /// wild, e.g., day or night.
    /// </summary>
    public class EncounterCondition : NamedApiResource
    {
        /// <summary>
        /// The identifier for this resource.
        /// </summary>
        public override int Id { get; set; }

        internal new static string ApiEndpoint { get; } = "encounter-condition";

        /// <summary>
        /// The name for this resource.
        /// </summary>
        public override string Name { get; set; } = null!;

        /// <summary>
        /// The name of this resource listed in different
        /// languages.
        /// </summary>
        public List<Names> Names { get; set; } = null!;

        /// <summary>
        /// A list of possible values for this encounter condition.
        /// </summary>
        public List<NamedApiResource<EncounterConditionValue>> Values { get; set; } = null!;
    }

    /// <summary>
    /// Encounter condition values are the various states that an encounter
    /// condition can have, i.e., time of day can be either day or night.
    /// </summary>
    public class EncounterConditionValue : NamedApiResource
    {
        /// <summary>
        /// The identifier for this resource.
        /// </summary>
        public override int Id { get; set; }

        internal new static string ApiEndpoint { get; } = "encounter-condition-value";

        /// <summary>
        /// The name for this resource.
        /// </summary>
        public override string Name { get; set; } = null!;

        /// <summary>
        /// The condition this encounter condition value pertains
        /// to.
        /// </summary>
        public NamedApiResource<EncounterCondition> Condition { get; set; } = null!;

        /// <summary>
        /// The name of this resource listed in different
        /// languages.
        /// </summary>
        public List<Names> Names { get; set; } = null!;
    }
}
