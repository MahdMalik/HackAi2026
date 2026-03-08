"""
PPO Training Script for ModelMystery Agents
Uses ML-Agents Python API for training Killer and Survivor agents
"""

import subprocess
import sys
import os

def train_agents(config_path="config.yaml", run_id="ModelMystery_Run", force=False):
    """
    Train the agents using ML-Agents PPO trainer
    
    Args:
        config_path: Path to the training config file
        run_id: Identifier for this training run
        force: Force retraining if checkpoints exist
    """
    
    # Command to run ML-Agents training
    cmd = [
        "mlagents-learn",
        config_path,
        "--run-id=" + run_id,
    ]
    
    if force:
        cmd.append("--force")
    
    print(f"Starting training with command: {' '.join(cmd)}")
    
    try:
        result = subprocess.run(cmd, check=False)  # Don't fail on exit code
        if result.returncode != 0:
            print(f"Training process exited with code {result.returncode}")
            print("Note: This may be due to ONNX export errors (PyTorch 2.10 compatibility).")
            print("Training models are still saved in results/ directory.")
            return False
        print("Training completed successfully!")
        return result.returncode == 0
    except subprocess.CalledProcessError as e:
        print(f"Training error: {e}")
        return False
    except FileNotFoundError:
        print("mlagents-learn not found. Please install ML-Agents:")
        print("  pip install mlagents")
        return False

def download_models(run_id="ModelMystery_Run"):
    """After training, convert the PyTorch models to ONNX for Unity"""
    print(f"To convert models to ONNX for Unity, add the folder 'results/{run_id}' to Unity")

if __name__ == "__main__":
    """
    Usage:
        python PPO.py                   # Basic training
        python PPO.py --force           # Force retrain
        python PPO.py --config custom.yaml --run-id MyRun
    """
    
    config = "config.yaml"
    run_id = "ModelMystery_Run"
    force = False
    
    # Parse command line arguments
    for arg in sys.argv[1:]:
        if arg == "--force":
            force = True
        elif arg.startswith("--config="):
            config = arg.split("=")[1]
        elif arg.startswith("--run-id="):
            run_id = arg.split("=")[1]
    
    print(f"""
    ========================================
    ModelMystery Agent Training
    ========================================
    Config: {config}
    Run ID: {run_id}
    Force Retrain: {force}
    ========================================
    """)
    
    success = train_agents(config, run_id, force)
    
    if success:
        print("\n" + "="*40)
        print("Training completed!")
        print("Models available in: results/" + run_id)
        print("="*40)
