using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Base.Global;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Concubines {
    internal sealed class MCMConfig : AttributeGlobalSettings<MCMConfig> {
        public override string Id => "Concubines";
        public override string DisplayName => "Concubines";
        public override string FolderName => "Concubines";
        public override string FormatType => "xml";

        // PLAYER

        [SettingPropertyInteger("Attraction Relation Needed", -200, 200, Order = 1, RequireRestart = false, HintText = "Lower number = easier to take concubines, bigger number = harder.")]
        [SettingPropertyGroup("Player")]
        public int AttractionRelationNeededPlayer { get; set; } = 80;

        // AI

        [SettingPropertyBool("Enable AI Concubines", Order = 1, RequireRestart = false, HintText = "Allow AI to be able to take concubines.")]
        [SettingPropertyGroup("AI")]
        public bool EnableAIConcubines { get; set; } = true;

        [SettingPropertyInteger("AI Attraction Relation Needed", -200, 200, Order = 2, RequireRestart = false, HintText = "Lower number = easier to take concubines, bigger number = harder.")]
        [SettingPropertyGroup("AI")]
        public int AttractionRelationNeededAI { get; set; } = 100;
    }
}
