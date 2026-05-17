using UnityEngine;

public class ParticlesHandler : MonoBehaviour{
    private GameObject currentParticleObj;
    private int currentIndex;
    private Camera mainCamera;
    private readonly Vector3 instantiatePosition = new(2f, 0f, 0f);

    private void Awake() {
        mainCamera = Camera.main;
    }

    internal void PlayParticle(GameObject particleGameObj) {
        if (currentParticleObj) {
            DestroyImmediate(currentParticleObj);
        }

        currentParticleObj = Instantiate(particleGameObj, instantiatePosition, Quaternion.identity, transform);
    }

    public void OnMouseUpAsButton() {
        Debug.Log("On mouse click");
    }
}