using UnityEngine;

/// <summary>
/// ทำให้ฟิลด์อ่านอย่างเดียวใน Inspector (ต้องใช้คู่กับ Drawer ถึงจะเห็นผล)
/// แต่แค่มีคลาสนี้ก็พอให้คอมไพล์ผ่านได้แล้ว
/// </summary>
public class ReadOnlyAttribute : PropertyAttribute { }
