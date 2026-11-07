using UnityEngine;

public class LookAtCamera : MonoBehaviour
{
    private Camera _camera;
    public bool normalFacing;

    // Переменные для блокировки вращения по осям
    public bool lockX = false;
    public bool lockY = false;
    public bool lockZ = false;

    private void Start()
    {
        _camera = Camera.main;
    }

    private void Update()
    {
        // Проверяем, есть ли камера
        if (_camera != null)
        {
            Quaternion targetRotation;

            if (normalFacing)
            {
                // Поворачиваем объект к камере с дополнительным поворотом на 180 градусов
                Vector3 directionToCamera = _camera.transform.position - transform.position;
                targetRotation = Quaternion.LookRotation(directionToCamera) * Quaternion.Euler(0, 180, 0);
            }
            else
            {
                // Поворачиваем объект к камере
                targetRotation = Quaternion.LookRotation(_camera.transform.position - transform.position);
            }

            // Применяем блокировку осей
            Vector3 eulerRotation = targetRotation.eulerAngles;

            if (lockX) eulerRotation.x = transform.rotation.eulerAngles.x; // Фиксируем угол по X
            if (lockY) eulerRotation.y = transform.rotation.eulerAngles.y; // Фиксируем угол по Y
            if (lockZ) eulerRotation.z = transform.rotation.eulerAngles.z; // Фиксируем угол по Z

            // Применяем итоговый поворот
            transform.rotation = Quaternion.Euler(eulerRotation);
        }
    }
}
