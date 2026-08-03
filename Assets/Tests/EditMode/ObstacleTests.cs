using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace FirstLevel.EditorTests
{
    public sealed class ObstacleTests
    {
        [Test]
        public void StartAppliesRandomizedSizeWithinConfiguredRange()
        {
            GameObject obstacleObject = CreateObstacle(out MonoBehaviour obstacle);
            SetPublicField(obstacle, "minSize", 1.25f);
            SetPublicField(obstacle, "maxSize", 1.25f);

            obstacle.SendMessage("Start");

            Assert.That(obstacleObject.transform.localScale, Is.EqualTo(new Vector3(1.25f, 1.25f, 1f)));
            Object.DestroyImmediate(obstacleObject);
        }

        [Test]
        public void PulseScalesObstacleFromBaseScale()
        {
            GameObject obstacleObject = CreateObstacle(out MonoBehaviour obstacle);
            SetPublicField(obstacle, "pulseScale", true);
            SetPublicField(obstacle, "pulseAmount", 0.25f);
            SetPublicField(obstacle, "pulseSpeed", 1f);
            SetPrivateField(obstacle, "baseScale", Vector3.one);
            SetPrivateField(obstacle, "spawnTime", Time.time - 0.25f);

            InvokePrivateMethod(obstacle, "Pulse");

            Assert.That(obstacleObject.transform.localScale.x, Is.EqualTo(1.25f).Within(0.0001f));
            Assert.That(obstacleObject.transform.localScale.y, Is.EqualTo(1.25f).Within(0.0001f));
            Object.DestroyImmediate(obstacleObject);
        }

        [Test]
        public void WrapAroundCameraMovesObstacleToOppositeHorizontalEdge()
        {
            using (CreateCamera(out Camera camera))
            {
                GameObject obstacleObject = CreateObstacle(out MonoBehaviour obstacle);
                SetPublicField(obstacle, "wrapAroundCameraBounds", true);
                SetPublicField(obstacle, "boundaryCamera", camera);
                SetPublicField(obstacle, "wrapMargin", 0.5f);
                obstacleObject.transform.position = new Vector3(-6f, 0f, 0f);

                InvokePrivateMethod(obstacle, "WrapAroundCamera");

                Assert.That(obstacleObject.transform.position.x, Is.EqualTo(5.5f).Within(0.0001f));
                Object.DestroyImmediate(obstacleObject);
            }
        }

        [Test]
        public void LaserHitShrinksObstacle()
        {
            GameObject obstacleObject = CreateObstacle(out MonoBehaviour obstacle);
            SetPublicField(obstacle, "laserHitsToDestroy", 4f);
            SetPublicField(obstacle, "minExplodeSize", 0.2f);
            SetPrivateField(obstacle, "baseScale", Vector3.one);
            SetPrivateField(obstacle, "currentLaserHealth", 4f);

            InvokePublicMethod(obstacle, "ApplyLaserHit", 1f, 0.25f);

            Assert.That(obstacleObject.transform.localScale.x, Is.EqualTo(0.75f).Within(0.0001f));
            Assert.That(obstacleObject.transform.localScale.y, Is.EqualTo(0.75f).Within(0.0001f));
            Object.DestroyImmediate(obstacleObject);
        }

        [Test]
        public void RespawnPositionStaysInsideCameraBounds()
        {
            using (CreateCamera(out Camera camera))
            {
                GameObject obstacleObject = CreateObstacle(out MonoBehaviour obstacle);
                SetPublicField(obstacle, "boundaryCamera", camera);
                SetPublicField(obstacle, "respawnInset", 1f);
                SetPublicField(obstacle, "playerAvoidRadius", 0f);

                Vector3 spawnPosition = (Vector3)InvokePrivateMethod(obstacle, "GetRandomInteriorSpawnPosition");

                Assert.That(spawnPosition.x, Is.InRange(-4f, 4f));
                Assert.That(spawnPosition.y, Is.InRange(-4f, 4f));
                Object.DestroyImmediate(obstacleObject);
            }
        }

        [Test]
        public void DestroyedObstacleAwardsPlayerScore()
        {
            GameObject playerObject = CreatePlayer(out MonoBehaviour player);
            SetPublicField(player, "scoreMultiplier", 0f);
            SetPublicField(player, "obstacleDestroyScore", 5);
            player.SendMessage("Start");

            GameObject obstacleObject = CreateObstacle(out MonoBehaviour obstacle);
            SetPublicField(obstacle, "laserHitsToDestroy", 1f);
            SetPublicField(obstacle, "minExplodeSize", 0.2f);
            SetPublicField(obstacle, "spawnReplacementOnDestroy", false);
            SetPublicField(obstacle, "explosionFragments", 1);
            SetPrivateField(obstacle, "baseScale", Vector3.one);
            SetPrivateField(obstacle, "currentLaserHealth", 1f);

            InvokePublicMethod(obstacle, "ApplyLaserHit", 1f, 0.25f);

            Assert.That(GetPublicField<float>(player, "score"), Is.EqualTo(5f));
            Object.DestroyImmediate(obstacleObject);
            GameObject fragment = GameObject.Find("Asteroid Explosion Fragment");
            if (fragment != null)
            {
                Object.DestroyImmediate(fragment);
            }
            Object.DestroyImmediate(playerObject);
        }

        private static GameObject CreateObstacle(out MonoBehaviour obstacle)
        {
            var obstacleObject = new GameObject("Obstacle");
            obstacleObject.AddComponent<Rigidbody2D>();
            obstacleObject.AddComponent<SpriteRenderer>();
            obstacle = (MonoBehaviour)obstacleObject.AddComponent(GetObstacleType());
            SetPublicField(obstacle, "minSpeed", 0f);
            SetPublicField(obstacle, "maxSpeed", 0f);
            SetPublicField(obstacle, "maxSpinSpeed", 0f);
            SetPublicField(obstacle, "pulseScale", false);
            SetPublicField(obstacle, "wrapAroundCameraBounds", false);
            return obstacleObject;
        }

        private static DisposableCamera CreateCamera(out Camera camera)
        {
            var cameraObject = new GameObject("Boundary Camera");
            camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.aspect = 1f;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            return new DisposableCamera(cameraObject);
        }

        private static void SetPublicField<T>(MonoBehaviour obstacle, string fieldName, T value)
        {
            FieldInfo field = obstacle.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public);
            Assert.That(field, Is.Not.Null);
            field.SetValue(obstacle, value);
        }

        private static void SetPrivateField<T>(MonoBehaviour obstacle, string fieldName, T value)
        {
            FieldInfo field = obstacle.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(obstacle, value);
        }

        private static T GetPublicField<T>(MonoBehaviour obstacle, string fieldName)
        {
            FieldInfo field = obstacle.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public);
            Assert.That(field, Is.Not.Null);
            return (T)field.GetValue(obstacle);
        }

        private static object InvokePrivateMethod(MonoBehaviour obstacle, string methodName)
        {
            MethodInfo method = obstacle.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            return method.Invoke(obstacle, null);
        }

        private static void InvokePublicMethod(MonoBehaviour obstacle, string methodName, params object[] arguments)
        {
            MethodInfo method = obstacle.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
            Assert.That(method, Is.Not.Null);
            method.Invoke(obstacle, arguments);
        }

        private static System.Type GetObstacleType()
        {
            System.Type obstacleType = System.Type.GetType("Obstacle, Assembly-CSharp");
            Assert.That(obstacleType, Is.Not.Null);
            return obstacleType;
        }

        private static GameObject CreatePlayer(out MonoBehaviour player)
        {
            var playerObject = new GameObject("Player");
            playerObject.AddComponent<Rigidbody2D>();

            var hull = new GameObject("Life_Support");
            hull.transform.SetParent(playerObject.transform, false);
            hull.AddComponent<SpriteRenderer>();

            player = (MonoBehaviour)playerObject.AddComponent(GetPlayerControllerType());
            SetPublicField(player, "stylizeShipOnStart", false);
            SetPublicField(player, "explosionFragments", 1);
            SetPublicField(player, "destroyDelay", 10f);
            return playerObject;
        }

        private static System.Type GetPlayerControllerType()
        {
            System.Type playerControllerType = System.Type.GetType("PlayerController, Assembly-CSharp");
            Assert.That(playerControllerType, Is.Not.Null);
            return playerControllerType;
        }

        private readonly struct DisposableCamera : System.IDisposable
        {
            private readonly GameObject cameraObject;

            public DisposableCamera(GameObject cameraObject)
            {
                this.cameraObject = cameraObject;
            }

            public void Dispose()
            {
                Object.DestroyImmediate(cameraObject);
            }
        }
    }
}
