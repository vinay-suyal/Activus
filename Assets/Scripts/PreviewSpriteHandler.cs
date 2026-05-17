using UnityEngine;

namespace DefaultNamespace{
    public class PreviewSpriteHandler : MonoBehaviour{
        private SpriteRenderer spriteRenderer;
        private BoxCollider2D boxCollider2D;
        private const int TOP_LAYER_SORTING_ORDER = 1000;
        private const int BOTTOM_LAYER_SORTING_ORDER = -1000;

        private void Awake() {
            spriteRenderer = GetComponent<SpriteRenderer>();
            boxCollider2D = GetComponent<BoxCollider2D>();
        }

        internal void RemoveSprite() {
            SetSprite(null);
        }

        internal void SetSprite(Sprite sprite) {
            spriteRenderer.sprite = sprite;
        }

        internal void SetAsTopLayer() {
            spriteRenderer.sortingOrder = TOP_LAYER_SORTING_ORDER;
            var currentPos = spriteRenderer.transform.position;
            currentPos.z = -9f;
            spriteRenderer.transform.position = currentPos;
        }

        internal void SetAsBottomLayer() {
            spriteRenderer.sortingOrder = BOTTOM_LAYER_SORTING_ORDER;
            var currentPos = spriteRenderer.transform.position;
            currentPos.z = 100f;
            spriteRenderer.transform.position = currentPos;
        }

        public void UpdateCollider(Bounds spriteBounds) {
            boxCollider2D.size = spriteBounds.size;
            boxCollider2D.offset = spriteBounds.center;
        }
    }
}