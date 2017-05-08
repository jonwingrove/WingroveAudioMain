# WingroveAudio
Event driven audio handler for Unity3d.
No documentation yet, sry.

# Explanation
Set up an hierarchy prefab to receive events, triggering audio appropriately as they happen.
The game code becomes as simple as:
WingroveAudio.WingroveRoot.Instance.PostEvent([EVENT NAME]);

# Parameters
Support for global & per-object parameters, can be mapped to volume/pitch/filter curves.
WingroveAudio.WingroveRoot.Instance.SetParameter([PARAM], [VALUE], (optional)[OBJECT]);

# Auto export event & parameter names to const look-up
Auto-generates C# file with event and parameter names for easy code writing.

# Event responses
Play/Play Random/Play Ordered (with fade), Pause, Resume, Stop (with fade), duck group.

# Misc
Instance limiting, random pitch variations, distance-based spatial blending, audio areas & more...

# NonCommercial License
WingroveAudio (c) 2017 by Jon Wingrove

WingroveAudio is licensed under a
Creative Commons Attribution-NonCommercial 3.0 Unported License.

You should have received a copy of the license along with this
work.  If not, see <http://creativecommons.org/licenses/by-nc/3.0/>.

# Commercial Licensing
Please contact me- but here's my general view:

* Games which are standard "pay up front" model with no in-app purchases: Use WingroveAudio restriction free
* Educational (with no ads or IAP): Use WingroveAudio restriction free
* Game or app with ads or any in app-purchases (e.g. 'free to play'): Paid licensing available
* Corporate or non-game: Paid licensing available

Unsure? contact me.
