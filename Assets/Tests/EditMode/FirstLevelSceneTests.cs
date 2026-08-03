using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FirstLevel.EditorTests
{
    public sealed class FirstLevelSceneTests
    {
        private const string ScenePath = "Assets/Settings/Scenes 1/FirstLevel.unity";

        [Test]
        public void FirstLevelSceneAssetExists()
        {
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);

            Assert.That(sceneAsset, Is.Not.Null);
        }

        [Test]
        public void GroundHasExpectedMarioStyleRunLength()
        {
            OpenFirstLevel();

            AssertTransform("Dirt Body", new Vector3(105f, -2f, 0f), new Vector3(210f, 4f, 1f));
            AssertTransform("Grass Top Play Surface", new Vector3(105f, 0.1f, -0.05f), new Vector3(210f, 0.2f, 1f));
            Assert.That(GameObject.Find("Grass Top Play Surface").GetComponent<BoxCollider2D>(), Is.Not.Null);
            AssertTransform("Level Begin", Vector3.zero, Vector3.one);
            AssertTransform("Level End", new Vector3(210f, 0f, 0f), Vector3.one);
        }

        [Test]
        public void FlagsMarkStartAndEnd()
        {
            OpenFirstLevel();

            AssertTransform("Start Flag Pole", new Vector3(2f, 1f, -0.1f), new Vector3(0.08f, 2f, 1f));
            AssertTransform("Start Flag Cloth", new Vector3(2.45f, 1.55f, -0.15f), new Vector3(0.85f, 0.5f, 1f));
            AssertTransform("End Flag Pole", new Vector3(208f, 1f, -0.1f), new Vector3(0.08f, 2f, 1f));
            AssertTransform("End Flag Cloth", new Vector3(208.45f, 1.55f, -0.15f), new Vector3(0.85f, 0.5f, 1f));
        }

        [Test]
        public void ShadingAccentsArePresent()
        {
            OpenFirstLevel();

            AssertTransform("Grass Top Highlight", new Vector3(105f, 0.18f, -0.12f), new Vector3(210f, 0.04f, 1f));
            AssertTransform("Grass Front Edge Shadow", new Vector3(105f, 0.01f, -0.14f), new Vector3(210f, 0.04f, 1f));
            AssertTransform("Dirt Upper Highlight", new Vector3(105f, -0.35f, -0.12f), new Vector3(210f, 0.12f, 1f));
            AssertTransform("Dirt Bottom Shadow", new Vector3(105f, -3.55f, -0.12f), new Vector3(210f, 0.3f, 1f));
            AssertTransform("Start Pole Shadow", new Vector3(2.03f, 1f, -0.2f), new Vector3(0.02f, 2f, 1f));
            AssertTransform("End Pole Shadow", new Vector3(208.03f, 1f, -0.2f), new Vector3(0.02f, 2f, 1f));
            AssertTransform("Start Flag Cloth Highlight", new Vector3(2.45f, 1.73f, -0.22f), new Vector3(0.85f, 0.08f, 1f));
            AssertTransform("Start Flag Cloth Shadow", new Vector3(2.45f, 1.37f, -0.23f), new Vector3(0.85f, 0.08f, 1f));
            AssertTransform("End Flag Cloth Highlight", new Vector3(208.45f, 1.73f, -0.22f), new Vector3(0.85f, 0.08f, 1f));
            AssertTransform("End Flag Cloth Shadow", new Vector3(208.45f, 1.37f, -0.23f), new Vector3(0.85f, 0.08f, 1f));
        }

        [Test]
        public void CameraUsesWhiteBackground()
        {
            OpenFirstLevel();

            var camera = GameObject.Find("Main Camera").GetComponent<Camera>();

            Assert.That(camera, Is.Not.Null);
            Assert.That(camera.orthographic, Is.True);
            Assert.That(camera.backgroundColor, Is.EqualTo(Color.white));
        }

        [Test]
        public void BordersAlignInsideMainCameraViewWithEqualThickness()
        {
            OpenFirstLevel();

            var camera = GameObject.Find("Main Camera").GetComponent<Camera>();
            const float expectedThickness = 0.25f;
            var halfHeight = camera.orthographicSize;
            var halfWidth = halfHeight * (16f / 9f);
            var left = camera.transform.position.x - halfWidth;
            var right = camera.transform.position.x + halfWidth;
            var bottom = camera.transform.position.y - halfHeight;
            var top = camera.transform.position.y + halfHeight;
            var center = new Vector2(camera.transform.position.x, camera.transform.position.y);

            AssertBorder("Top Border", new Vector3(center.x, top - expectedThickness / 2f, -0.3f), new Vector3(halfWidth * 2f, expectedThickness, 1f), expectedThickness);
            AssertBorder("Bottom Border", new Vector3(center.x, bottom + expectedThickness / 2f, -0.3f), new Vector3(halfWidth * 2f, expectedThickness, 1f), expectedThickness);
            AssertBorder("Left Border", new Vector3(left + expectedThickness / 2f, center.y, -0.3f), new Vector3(expectedThickness, halfHeight * 2f, 1f), expectedThickness);
            AssertBorder("Right Border", new Vector3(right - expectedThickness / 2f, center.y, -0.3f), new Vector3(expectedThickness, halfHeight * 2f, 1f), expectedThickness);
        }

        private static void OpenFirstLevel()
        {
            EditorSceneManager.OpenScene(ScenePath);
        }

        private static void AssertTransform(string objectName, Vector3 expectedPosition, Vector3 expectedScale)
        {
            var gameObject = GameObject.Find(objectName);

            Assert.That(gameObject, Is.Not.Null, objectName);
            AssertVector3(gameObject.transform.position, expectedPosition, $"{objectName} position");
            AssertVector3(gameObject.transform.localScale, expectedScale, $"{objectName} scale");
        }

        private static void AssertBorder(string objectName, Vector3 expectedPosition, Vector3 expectedScale, float expectedThickness)
        {
            AssertTransform(objectName, expectedPosition, expectedScale);

            var border = GameObject.Find(objectName);
            Assert.That(border.GetComponent<BoxCollider2D>(), Is.Not.Null, $"{objectName} collider");
            Assert.That(Mathf.Min(border.transform.localScale.x, border.transform.localScale.y), Is.EqualTo(expectedThickness).Within(0.0001f), $"{objectName} thickness");
        }

        private static void AssertVector3(Vector3 actual, Vector3 expected, string message)
        {
            Assert.That(Vector3.Distance(actual, expected), Is.LessThan(0.0001f), message);
        }
    }
}
