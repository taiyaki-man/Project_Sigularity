using UnityEngine;

namespace ProjectSingularity.Dialogues
{
    public enum InvestigationPhase
    {
        Phase0, // 操作開始〜シグレが殺した真実を突き止めるまで
        Phase1, // シグレが殺した真実を突き止めた〜シグレの動機を突き止めるまで
        Phase2, // シグレの動機を突き止めた〜シグレの知らない事実を突き止めるまで
        Phase3, // 最終局面開始〜エンディング分岐まで
    }

    [CreateAssetMenu(menuName = "ProjectSingularity/Dialogue/Investigation State",fileName = "InvestigationState")]
    public class InvestigationStateSO : ScriptableObject
    {
        [Header("Phase")]
        public InvestigationPhase phase = InvestigationPhase.Phase0;

        [Header("Discovered Facts (Flags)")]
        public bool discoveredMurderTruth = false; // シグレが殺した真実
        public bool discoveredMotive = false;      // シグレの動機
        public bool discoveredHiddenTruth = false; // シグレの知らない事実

        [Range(0, 100)]
        public int progress = 0;

        [Header("Social Params")]
        [Range(0, 100)] public int trust = 0;    // シグレの信頼度

        /// <summary>flagからPhaseを自動的に更新する</summary>
        public void EvaluatePhase()
        {
            if (discoveredHiddenTruth)
            {
                phase = InvestigationPhase.Phase3;
                return;
            }

            if (discoveredMotive)
            {
                phase = InvestigationPhase.Phase2;
                return;
            }

            if (discoveredMurderTruth)
            {
                phase = InvestigationPhase.Phase1;
                return;
            }

            phase = InvestigationPhase.Phase0;
        }

    }
}