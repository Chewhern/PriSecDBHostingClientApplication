﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PriSecDBClientPanel.Model
{
    class LoginModels
    {
        public String RequestStatus { get; set; }

        public String SignedRandomChallengeBase64String { get; set; }

        public String ServerECDSAPKBase64String { get; set; }
    }
}
