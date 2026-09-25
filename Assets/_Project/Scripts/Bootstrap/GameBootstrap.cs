using UnityEngine;
using Basket.Core;
using Basket.Gameplay;
using Basket.AI;
using Basket.Input;
using Basket.UI;

namespace Basket.Bootstrap
{
    public class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private PlayerMotor humanMotor;
        [SerializeField] private HumanInputProvider humanInput;
        [SerializeField] private PlayerMotor aiMotor;
        [SerializeField] private AIOpponentController aiController;
        [SerializeField] private BallController ball;
        [SerializeField] private PassSystem passSystem;
        [SerializeField] private ShootingSystem shootingSystem;
        [SerializeField] private DribbleSystem dribbleSystem;
        [SerializeField] private MatchManager matchManager;
        [SerializeField] private ScoreTrigger scoreTrigger;
        [SerializeField] private CameraController cameraController;
        [SerializeField] private DebugHud debugHud;
        [SerializeField] private BallConfig ballConfig;
        [SerializeField] private ShotConfig shotConfig;
        [SerializeField] private Transform rimTarget;

        private void Awake()
        {
            matchManager.Configure(ball);
            scoreTrigger.Configure(ball);
            passSystem.Configure(ball, ballConfig);
            shootingSystem.Configure(ball, shotConfig, rimTarget);
            dribbleSystem.Configure(ball);
            cameraController.Configure(humanMotor.transform);
            debugHud.Configure(matchManager.State, ball, aiController);
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            var perception = new AIPerception(
                selfPosition: aiMotor.transform.position,
                opponentPosition: humanMotor.transform.position,
                ballPosition: ball.Position,
                opponentHasBall: ball.CurrentHolder == humanMotor.transform,
                selfHasBall: ball.CurrentHolder == aiMotor.transform);
            aiController.Tick(perception);

            TickAgent(humanInput, humanMotor, humanMotor.transform, aiMotor.transform, dt);
            TickAgent(aiController, aiMotor, aiMotor.transform, humanMotor.transform, dt);

            // Reliable pickup path for a loose ball -- see the comment on
            // BallController.TryCatchNearby for why this can't be left to
            // OnCollisionEnter alone when a CharacterController is involved.
            ball.TryCatchNearby(humanMotor.transform);
            ball.TryCatchNearby(aiMotor.transform);
        }

        private void TickAgent(IPlayerAgent agent, PlayerMotor motor, Transform self, Transform other, float dt)
        {
            motor.Tick(agent.GetMoveInput(), agent.WantsSprint(), dt);

            // DribbleSystem is a single shared instance ticked from both agents' calls this
            // method makes every frame. Gating on "am I the current holder" (like the
            // pass/shoot calls below already do) is required, not optional: without it, the
            // non-holder's call runs dribbleSystem.Tick with its OWN movement state every
            // frame too, and since DribbleSystem only checks ball.CurrentState (not which
            // agent holds it), the non-holder's call would either reset the holder's bounce
            // offset to zero (if the non-holder is stationary) or double the bounce
            // frequency (if both are moving) -- a real, silent bug, found during Task 18's
            // integration review.
            if (ball.CurrentHolder == self)
            {
                dribbleSystem.Tick(agent.GetMoveInput().sqrMagnitude > 0.01f, dt);
            }

            if (agent.WantsPass() && ball.CurrentHolder == self)
            {
                passSystem.TryPass(self, other);
            }
            if (agent.WantsShoot() && ball.CurrentHolder == self)
            {
                shootingSystem.TryShoot(self);
            }
        }
    }
}
