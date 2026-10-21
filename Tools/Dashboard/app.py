# ==============================================================================
# File Name: app.py
# Version: 1.0.0
# Made And Checked By DELTA SYNTH & All Code Agentic AI Engine
# Location: Tools/Dashboard/app.py
# Description: Voicebank & Thai Phoneme Analytical Dashboard (Building Data Apps)
# ==============================================================================

import os
import re
from pathlib import Path

try:
    import streamlit as st
    import pandas as pd
    import plotly.express as px
except ImportError:
    st = None

def parse_oto_file(oto_path: str):
    """Parse UTAU oto.ini into structured records."""
    records = []
    if not os.path.exists(oto_path):
        return pd.DataFrame()

    encodings = ['utf-8', 'shift_jis', 'cp874', 'latin1']
    content = ""
    for enc in encodings:
        try:
            with open(oto_path, 'r', encoding=enc) as f:
                content = f.read()
            break
        except UnicodeDecodeError:
            continue

    for line in content.splitlines():
        line = line.strip()
        if not line or '=' not in line:
            continue
        wav_file, rest = line.split('=', 1)
        parts = rest.split(',')
        alias = parts[0] if len(parts) > 0 and parts[0] else wav_file.replace('.wav', '')
        offset = float(parts[1]) if len(parts) > 1 and parts[1] else 0.0
        consonant = float(parts[2]) if len(parts) > 2 and parts[2] else 0.0
        cutoff = float(parts[3]) if len(parts) > 3 and parts[3] else 0.0
        preutterance = float(parts[4]) if len(parts) > 4 and parts[4] else 0.0
        overlap = float(parts[5]) if len(parts) > 5 and parts[5] else 0.0

        records.append({
            "Wav": wav_file,
            "Alias": alias,
            "Offset": offset,
            "Consonant": consonant,
            "Cutoff": cutoff,
            "Preutterance": preutterance,
            "Overlap": overlap
        })

    return pd.DataFrame(records)

def main():
    if st is None:
        print("[ERROR] streamlit, pandas, and plotly are required. Run: pip install streamlit pandas plotly")
        return

    st.set_page_config(
        page_title="DELTA SYNTH — Voicebank & Phoneme Analytics",
        page_icon="🎵",
        layout="wide",
        initial_sidebar_state="expanded"
    )

    # DELTA SYNTH Custom Theme Styling
    st.markdown("""
        <style>
        .main { background-color: #1A1A1A; color: #F0F0F0; font-family: 'Leelawadee UI', sans-serif; }
        h1, h2, h3 { color: #CC2200; font-family: 'Leelawadee UI', sans-serif; }
        .stMetric { background-color: #262626; padding: 10px; border-radius: 8px; border-left: 4px solid #CC2200; }
        </style>
    """, unsafe_allow_html=True)

    st.title("🎵 DELTA SYNTH — Thai OpenUtau Analytics Dashboard")
    st.markdown("**Version 1.0.0** | Made And Checked By DELTA SYNTH & All Code Agentic AI Engine")

    st.sidebar.header("⚙️ Configuration")
    vb_dir = st.sidebar.text_input("Voicebank Directory Path:", value=os.getcwd())

    oto_files = []
    if os.path.isdir(vb_dir):
        for root, _, files in os.walk(vb_dir):
            for file in files:
                if file.lower() == "oto.ini":
                    oto_files.append(os.path.join(root, file))

    if not oto_files:
        st.info("ℹ️ No `oto.ini` files found in the specified path. Please select a valid voicebank directory.")
        st.stop()

    selected_oto = st.sidebar.selectbox("Select oto.ini File:", oto_files)
    df = parse_oto_file(selected_oto)

    if df.empty:
        st.warning("⚠️ Selected oto.ini contains no entries or could not be parsed.")
        st.stop()

    col1, col2, col3, col4 = st.columns(4)
    col1.metric("Total Samples", len(df))
    col2.metric("Avg Preutterance (ms)", f"{df['Preutterance'].mean():.2f}")
    col3.metric("Avg Consonant (ms)", f"{df['Consonant'].mean():.2f}")
    col4.metric("Avg Overlap (ms)", f"{df['Overlap'].mean():.2f}")

    st.subheader("📊 Phoneme Timing Distribution")
    fig = px.scatter(
        df,
        x="Preutterance",
        y="Overlap",
        color="Consonant",
        hover_data=["Alias", "Wav"],
        title="Preutterance vs Overlap Correlation",
        template="plotly_dark",
        color_continuous_scale="Reds"
    )
    st.plotly_chart(fig, use_container_width=True)

    st.subheader("📋 Sample Details & Configuration")
    search = st.text_input("Search Alias / Phoneme:")
    if search:
        filtered_df = df[df['Alias'].str.contains(search, case=False, na=False)]
    else:
        filtered_df = df

    st.dataframe(filtered_df, use_container_width=True)

if __name__ == "__main__":
    main()
