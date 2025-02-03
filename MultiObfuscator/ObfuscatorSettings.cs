using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultiObfuscator
{
    public class ObfuscatorSettings
    {
        /// <summary>
        /// A value between 0.0 and 1.0 representing the probability that dead code will be injected.
        /// </summary>
        public double DeadCodeProbability { get; set; } = 0.3;

        /// <summary>
        /// When true, control–flow flattening is applied to non–public methods.
        /// </summary>
        public bool EnableControlFlowFlattening { get; set; } = true;

        /// <summary>
        /// When true, string literals are split into parts before encryption.
        /// </summary>
        public bool EnableStringSplitting { get; set; } = true;
    }
}
