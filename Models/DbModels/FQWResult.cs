using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasySECv2.Models.DbModels
{
    public record FQWResult
    (
        // 1.1
        int ExamAdmittedAll, int ExamAdmittedFull, int ExamAdmittedMixed, int ExamAdmittedPart,
        int ExamA, int ExamB, int ExamC, int ExamD,
        int ExamAbsentAll, int ExamAbsentFull, int ExamAbsentMixed, int ExamAbsentPart,

        // 2.1 – 2.5
        int FqwAcceptedAll, int FqwAcceptedFull, int FqwAcceptedMixed, int FqwAcceptedPart,
        int FqwDefendedAll, int FqwDefendedFull, int FqwDefendedMixed, int FqwDefendedPart,
        int FqwA, int FqwB, int FqwC, int FqwD,
        int FqwAbsentAll, int FqwAbsentFull, int FqwAbsentMixed, int FqwAbsentPart,
        int FqwPostponedAll, int FqwPostponedFull, int FqwPostponedMixed, int FqwPostponedPart,

        // 2.6 – 2.9
        int FqwResearch, int FqwPractice, int FqwProject, int FqwStartup, int FqwSocial,
        int FqwToPublish, int FqwToImplement, int FqwImplemented,
        int HonourDiplomas,
        double? AvgOriginality
    );
}
