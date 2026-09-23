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
        }

        private void TickAgent(IPlayerAgent agent, PlayerMotor motor, Transform self, Transform other, float dt)
        {
            motor.Tick(agent.GetMoveInput(), agent.WantsSprint(), dt);
            dribbleSystem.Tick(agent.GetMoveInput().sqrMagnitude > 0.01f, dt);

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
