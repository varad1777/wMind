export const CORRELATION_RULES = [

  // =========================
  // POWER & ELECTRICAL ISSUES
  // =========================

  {
    id: "POWER_SUPPLY_INSTABILITY",
    when: ["Voltage_LOW", "Voltage_HIGH", "Current_HIGH"],
    cause: "Unstable power supply or incoming electrical disturbance",
    confidence: 0.8
  },

  {
    id: "UNDER_VOLTAGE_OVER_CURRENT",
    when: ["Voltage_LOW", "Current_HIGH"],
    cause: "Motor drawing excess current due to under-voltage or supply sag",
    confidence: 0.75
  },

  {
    id: "OVER_VOLTAGE_STRESS",
    when: ["Voltage_HIGH", "Current_HIGH"],
    cause: "Electrical stress caused by over-voltage condition",
    confidence: 0.7
  },

  {
    id: "POWER_FLUCTUATION",
    when: ["Voltage_FLUCTUATING", "Current_FLUCTUATING"],
    cause: "Intermittent power source or loose electrical connections",
    confidence: 0.8
  },

  // =========================
  // SENSOR & WIRING FAULTS
  // =========================

  {
    id: "CURRENT_SENSOR_FAULT",
    when: ["Current_NEGATIVE"],
    cause: "Faulty current sensor, wiring reversal, or ADC calibration issue",
    confidence: 0.9
  },

  {
    id: "VOLTAGE_SENSOR_FAULT",
    when: ["Voltage_NEGATIVE"],
    cause: "Voltage sensor malfunction or wiring fault",
    confidence: 0.9
  },

  {
    id: "FLOW_SENSOR_FAULT",
    when: ["FlowRate_NEGATIVE"],
    cause: "Flow sensor error or incorrect signal scaling",
    confidence: 0.85
  },

  {
    id: "MULTI_SENSOR_WIRING_ISSUE",
    when: ["Voltage_FLUCTUATING", "Current_NEGATIVE"],
    cause: "Common wiring, grounding, or signal reference issue",
    confidence: 0.85
  },

  // =========================
  // MECHANICAL LOAD & DRIVE
  // =========================

  {
    id: "MECHANICAL_OVERLOAD",
    when: ["Current_HIGH", "RPM_LOW"],
    cause: "Mechanical overload, jammed shaft, or excessive load",
    confidence: 0.8
  },

  {
    id: "MECHANICAL_SLIP",
    when: ["RPM_HIGH", "Torque_LOW"],
    cause: "Mechanical slippage or coupling failure",
    confidence: 0.75
  },

  {
    id: "EXCESSIVE_TORQUE_LOAD",
    when: ["Torque_HIGH", "Current_HIGH"],
    cause: "Excessive mechanical resistance or bearing failure",
    confidence: 0.8
  },

  // =========================
  // THERMAL ISSUES
  // =========================

  {
    id: "OVERHEATING_ELECTRICAL",
    when: ["Temperature_HIGH", "Current_HIGH"],
    cause: "Overheating due to sustained high current draw",
    confidence: 0.8
  },

  {
    id: "COOLING_FAILURE",
    when: ["Temperature_HIGH", "FlowRate_LOW"],
    cause: "Insufficient cooling flow or cooling system failure",
    confidence: 0.85
  },

  {
    id: "THERMAL_INSTABILITY",
    when: ["Temperature_FLUCTUATING"],
    cause: "Unstable thermal control or intermittent cooling",
    confidence: 0.7
  },

  // =========================
  // HYDRAULIC / PROCESS FLOW
  // =========================

  {
    id: "FLOW_BLOCKAGE",
    when: ["FlowRate_LOW", "Pressure_HIGH"],
    cause: "Partial blockage or downstream restriction",
    confidence: 0.85
  },

  {
    id: "CAVITATION_RISK",
    when: ["FlowRate_FLUCTUATING", "Vibration_HIGH"],
    cause: "Cavitation or air ingress in the system",
    confidence: 0.75
  },

  // =========================
  // VIBRATION & MECHANICAL HEALTH
  // =========================

  {
    id: "MECHANICAL_IMBALANCE",
    when: ["Vibration_HIGH", "RPM_HIGH"],
    cause: "Rotor imbalance or misalignment",
    confidence: 0.8
  },

  {
    id: "BEARING_FAILURE",
    when: ["Vibration_HIGH", "Temperature_HIGH"],
    cause: "Bearing wear or lubrication failure",
    confidence: 0.85
  },

  {
    id: "STRUCTURAL_RESONANCE",
    when: ["Vibration_FLUCTUATING", "Frequency_FLUCTUATING"],
    cause: "Resonance due to structural or mounting issues",
    confidence: 0.75
  },

  // =========================
  // FREQUENCY / DRIVE CONTROL
  // =========================

  {
    id: "DRIVE_CONTROL_INSTABILITY",
    when: ["Frequency_FLUCTUATING", "RPM_FLUCTUATING"],
    cause: "Unstable VFD or control loop tuning issue",
    confidence: 0.8
  },

  {
    id: "FREQUENCY_OUT_OF_RANGE",
    when: ["Frequency_HIGH", "RPM_HIGH"],
    cause: "Drive frequency exceeding safe operating limits",
    confidence: 0.75
  }

];
